# News Feed Tutorial: One Cell to Rule Them All

Building a news feed with mixed content types (text posts, images, videos, articles, ads) is a common requirement. With DrawnUI, you get the freedom to **just draw what you need** using one smart cell that adapts to any content type.

This tutorial demonstrates a challenging case of **uneven row heights** with real internet images and avatars, proper caching strategies, and performance optimizations including shadows and spacing techniques.
 
## The DrawnUI Way: One Universal Cell

With DrawnUI, we use one smart cell that simply shows or hides elements based on content type, not using any `DataTemplateSelector`. All recycling and height calculation happens automatically:

### 1. Define Content Types

```csharp
namespace Sandbox.Models;

public enum NewsType
{
    Text,
    Image,
    Video,
    Article,
    Ad
}

public class NewsItem
{
    public long Id { get; set; }
    public NewsType Type { get; set; }
    public string Title { get; set; }
    public string Content { get; set; }
    public string ImageUrl { get; set; }
    public string VideoUrl { get; set; }
    public string ArticleUrl { get; set; }
    public string AuthorName { get; set; }
    public string AuthorAvatarUrl { get; set; }  // Real avatar from RandomUser.me API
    public DateTime PublishedAt { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
}
```

### 2. Create A Universal Cell

> **Caching Strategy Note**: This example uses **uneven row heights** which requires `UseCache="Image"` cache type. If all cells had the same height, we could use `UseCache="ImageDoubleBuffered"` for even smoother performance.

> **Shadow Performance**: Shadows are cached inside cell padding to avoid performance issues. The cell padding creates space for shadows to render properly.

> **Spacing Strategy**: Stack spacing is set to 0 because the cell padding acts as general spacing between items.

```xml
<?xml version="1.0" encoding="utf-8"?>

<draw:SkiaDynamicDrawnCell
    x:Class="Sandbox.Views.NewsCell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:draw="http://schemas.appomobi.com/drawnUi/2023/draw"
    HorizontalOptions="Fill"
    UseCache="Image"
    Padding="16,6,16,10">

    <draw:SkiaLayout
        BackgroundColor="White"
        Padding="16"
        Type="Column" Spacing="12"
        HorizontalOptions="Fill">

        <draw:SkiaShape.VisualEffects>
            <draw:DropShadowEffect
                Color="#33000000" Blur="3" X="2" Y="2" />
        </draw:SkiaShape.VisualEffects>

        <!-- Author Header -->
        <draw:SkiaLayout Type="Row" Spacing="8"
                         UseCache="Image"
                         HorizontalOptions="Fill">

            <!--avatar image-->
            <draw:SkiaShape
                UseCache="Image"
                x:Name="AvatarFrame"
                Type="Circle"
                WidthRequest="40"
                HeightRequest="40"
                BackgroundColor="LightGray">

                <draw:SkiaImage
                    x:Name="AvatarImage"
                    Aspect="AspectFill"
                    HorizontalOptions="Fill"
                    VerticalOptions="Fill" />

            </draw:SkiaShape>
                
            <!--avatar initials-->
            <draw:SkiaLayout Type="Column"
                             UseCache="Operations"
                             HorizontalOptions="Fill">
                <draw:SkiaLabel
                    x:Name="AuthorLabel"
                    FontSize="14"
                    FontAttributes="Bold"
                    TextColor="Black" />
                <draw:SkiaLabel
                    x:Name="TimeLabel"
                    FontSize="12"
                    TextColor="Gray" />
            </draw:SkiaLayout>
        </draw:SkiaLayout>

        <!-- Content Title -->
        <draw:SkiaRichLabel
            UseCache="Operations"
            x:Name="TitleLabel"
            FontSize="16"
            FontAttributes="Bold"
            TextColor="Black"
            IsVisible="False" />

        <!-- Text Content -->
        <draw:SkiaRichLabel
            UseCache="Operations"
            x:Name="ContentLabel"
            FontSize="14"
            TextColor="#333"
            LineBreakMode="WordWrap"
            IsVisible="False" />

        <!--
        💡 SkiaLabel vs SkiaRichLabel:
        - Use SkiaLabel when your font family supports all the symbols you need
        - Use SkiaRichLabel for complex content with Unicode symbols (emojis, special chars)
        - SkiaRichLabel auto-finds installed fonts for missing symbols instead of showing "???"
        - Fallback symbols are customizable via SkiaLabel properties if needed
        -->

        <!-- Image Content -->
        <draw:SkiaImage
            x:Name="ContentImage"
            Aspect="AspectFill"
            HeightRequest="200"
            IsVisible="False" />
            
        <!-- Video Thumbnail with Play Button -->
        <draw:SkiaLayout
            HorizontalOptions="Fill"
            UseCache="Image"
            x:Name="VideoLayout"
            Type="Absolute"
            HeightRequest="200"
            IsVisible="False">

            <draw:SkiaImage
                x:Name="VideoThumbnail"
                Aspect="AspectFill"
                HorizontalOptions="Fill"
                VerticalOptions="Fill" />

            <!--wrapper to cache shadow-->
            <draw:SkiaLayout
                UseCache="Image"
                Padding="20"
                HorizontalOptions="Center"
                VerticalOptions="Center">

                <draw:SkiaShape
                    Type="Circle"
                    WidthRequest="60"
                    HeightRequest="60"
                    BackgroundColor="Black"
                    Opacity="0.7"
                    HorizontalOptions="Center"
                    VerticalOptions="Center">

                </draw:SkiaShape>

                <draw:SkiaRichLabel
                    Text="▶"
                    Opacity="0.7"
                    FontSize="26"
                    TextColor="White"
                    HorizontalOptions="Center"
                    VerticalOptions="Center" />

                <!-- 💡 Could be SkiaSvg with a crisp play icon or SkiaLottie with animation! -->

            </draw:SkiaLayout>

        </draw:SkiaLayout>
        <!-- Article Preview -->
        <draw:SkiaLayout
            HorizontalOptions="Fill"
            UseCache="Image"
            x:Name="ArticleLayout"
            Type="Row"
            Spacing="12"
            IsVisible="False">

            <draw:SkiaImage
                x:Name="ArticleThumbnail"
                WidthRequest="80"
                HeightRequest="80"
                Aspect="AspectFill" />

            <draw:SkiaLayout Type="Column" HorizontalOptions="Fill">
                <draw:SkiaLabel
                    x:Name="ArticleTitle"
                    FontSize="14"
                    FontAttributes="Bold"
                    TextColor="Black"
                    LineBreakMode="TailTruncation"
                    MaxLines="2" />
                <draw:SkiaLabel
                    x:Name="ArticleDescription"
                    FontSize="12"
                    TextColor="Gray"
                    LineBreakMode="TailTruncation"
                    MaxLines="3" />
            </draw:SkiaLayout>
        </draw:SkiaLayout>

        <!-- Ad Content -->
        <draw:SkiaLayout
            HorizontalOptions="Fill"
            UseCache="Image"
            x:Name="AdLayout"
            Type="Column"
            Spacing="8"
            IsVisible="False">

            <draw:SkiaLabel
                Text="Sponsored"
                FontSize="10"
                TextColor="Gray"
                HorizontalOptions="End" />

            <draw:SkiaImage
                x:Name="AdImage"
                Aspect="AspectFill"
                HeightRequest="150" />

            <draw:SkiaLabel
                x:Name="AdTitle"
                FontSize="14"
                FontAttributes="Bold"
                TextColor="Black" />
        </draw:SkiaLayout>
        <!-- Interaction Bar -->
        <draw:SkiaLayout Type="Row"
                         UseCache="Operations"
                         Spacing="16" HorizontalOptions="Fill">

            <draw:SkiaButton
                x:Name="LikeButton"
                Text="👍"
                BackgroundColor="Transparent"
                TextColor="Gray"
                FontSize="14" />

            <draw:SkiaButton
                x:Name="CommentButton"
                Text="💬"
                BackgroundColor="Transparent"
                TextColor="Gray"
                FontSize="14" />

            <draw:SkiaButton
                x:Name="ShareButton"
                Text="📤"
                BackgroundColor="Transparent"
                TextColor="Gray"
                FontSize="14"
                HorizontalOptions="End" />

        <!--
        💡 Pro Tip: You could use SkiaSvg for crisp vector icons (like, share, play button)
        or even SkiaLottie for awesome animated icons! We're keeping it simple with
        emoji for this tutorial, but the possibilities are endless!
        -->


        </draw:SkiaLayout>

    </draw:SkiaLayout>

    <draw:SkiaLabel
        Margin="24"
        x:Name="DebugId"
        TextColor="Red"
        HorizontalOptions="End" UseCache="Operations"/>

</draw:SkiaDynamicDrawnCell>
```

### 3. Why SkiaDynamicDrawnCell?

`SkiaDynamicDrawnCell` is optional but we prefer it because it's a lightweight helper that handles context changes smoothly. You could use a regular `SkiaLayout` instead, but then you'd miss out on some nice conveniences.

**What it gives you:**
- **Easy override methods** like `SetContent()` instead of manually handling item changes
- **Automatic size refresh** when content changes - it detects when the cell size changes and refreshes any auto-sized controls inside to prevent them from keeping old sizes when new content arrives
- **Cleaner code** without manual height calculations

The magic happens in just a few lines that check if the measured size changed and refresh everything accordingly. It's simple but saves you from weird sizing bugs when cells get recycled with different content.

*Want to see exactly how it works? Check out the source code in `SkiaDynamicDrawnCell.cs` - it's pretty straightforward!*

### 4. Smart Cell Logic - Content-Driven Behavior

```csharp
using DrawnUi.Controls;
using Sandbox.Models;

namespace Sandbox.Views;

public partial class NewsCell : SkiaDynamicDrawnCell
{
    public NewsCell()
    {
        InitializeComponent();
    }

    protected override void SetContent(object ctx)
    {
        base.SetContent(ctx);

        if (ctx is NewsItem news)
        {
            ConfigureForContentType(news);
        }
    }

    private void ConfigureForContentType(NewsItem news)
    {
        // Reset all content visibility
        HideAllContent();

        // Configure common elements
        DebugId.Text = $"{news.Id}";
        AuthorLabel.Text = news.AuthorName;
        TimeLabel.Text = GetRelativeTime(news.PublishedAt);
        AvatarImage.Source = news.AuthorAvatarUrl;
        LikeButton.Text = $"👍 {news.LikesCount}";
        CommentButton.Text = $"💬 {news.CommentsCount}";

        // Configure based on content type
        switch (news.Type)
        {
            case NewsType.Text:
                ConfigureTextPost(news);
                break;

            case NewsType.Image:
                ConfigureImagePost(news);
                break;

            case NewsType.Video:
                ConfigureVideoPost(news);
                break;

            case NewsType.Article:
                ConfigureArticlePost(news);
                break;

            case NewsType.Ad:
                ConfigureAdPost(news);
                break;
        }
    }

    private void HideAllContent()
    {
        TitleLabel.IsVisible = false;
        ContentLabel.IsVisible = false;
        ContentImage.IsVisible = false;
        VideoLayout.IsVisible = false;
        ArticleLayout.IsVisible = false;
        AdLayout.IsVisible = false;
    }

    private void ConfigureTextPost(NewsItem news)
    {
        if (!string.IsNullOrEmpty(news.Title))
        {
            TitleLabel.Text = news.Title;
            TitleLabel.IsVisible = true;
        }

        ContentLabel.Text = news.Content;
        ContentLabel.IsVisible = true;
    }

    private void ConfigureImagePost(NewsItem news)
    {
        ContentImage.Source = news.ImageUrl;
        ContentImage.IsVisible = true;

        if (!string.IsNullOrEmpty(news.Content))
        {
            ContentLabel.Text = news.Content;
            ContentLabel.IsVisible = true;
        }
    }

    private void ConfigureVideoPost(NewsItem news)
    {
        VideoThumbnail.Source = ExtractVideoThumbnail(news.VideoUrl);
        VideoLayout.IsVisible = true;

        if (!string.IsNullOrEmpty(news.Content))
        {
            ContentLabel.Text = news.Content;
            ContentLabel.IsVisible = true;
        }
    }

    private void ConfigureArticlePost(NewsItem news)
    {
        ArticleThumbnail.Source = news.ImageUrl;
        ArticleTitle.Text = news.Title;
        ArticleDescription.Text = news.Content;
        ArticleLayout.IsVisible = true;
    }

    private void ConfigureAdPost(NewsItem news)
    {
        AdImage.Source = news.ImageUrl;
        AdTitle.Text = news.Title;
        AdLayout.IsVisible = true;
    }

    private string GetRelativeTime(DateTime publishedAt)
    {
        var delta = DateTime.Now - publishedAt;
        return delta.TotalDays >= 1
            ? publishedAt.ToString("MMM dd")
            : delta.TotalHours >= 1
                ? $"{(int)delta.TotalHours}h"
                : $"{(int)delta.TotalMinutes}m";
    }

    private string ExtractVideoThumbnail(string videoUrl)
    {
        // Extract thumbnail from video URL or use placeholder
        return videoUrl; // For now, just use the same URL as it's from Picsum
    }
}
```

### 5. Real Internet Images Data Provider

> **Real Avatar Images**: Uses RandomUser.me API for 100x100px professional avatars
> **Real Content Images**: Uses Picsum Photos API for high-quality random images
> **Proper Image Preloading**: Both avatars and content images are preloaded for smooth scrolling

```csharp
using Sandbox.Models;

namespace Sandbox.Services;

public class NewsDataProvider
{
    private static Random random = new Random();
    private long index = 0;

    private static (string name, string avatarUrl)[] authors = new (string, string)[]
    {
        ("Alex Chen", "https://randomuser.me/api/portraits/men/1.jpg"),
        ("Sarah Williams", "https://randomuser.me/api/portraits/women/2.jpg"),
        ("Mike Johnson", "https://randomuser.me/api/portraits/men/3.jpg"),
        ("Emma Davis", "https://randomuser.me/api/portraits/women/4.jpg"),
        ("Chris Brown", "https://randomuser.me/api/portraits/men/5.jpg"),
        ("Lisa Martinez", "https://randomuser.me/api/portraits/women/6.jpg"),
        ("David Wilson", "https://randomuser.me/api/portraits/men/7.jpg"),
        ("Amy Garcia", "https://randomuser.me/api/portraits/women/8.jpg"),
        ("Tom Anderson", "https://randomuser.me/api/portraits/men/9.jpg"),
        ("Maya Patel", "https://randomuser.me/api/portraits/women/10.jpg")
    };

    private static string[] postTexts = new string[]
    {
        "Just finished an amazing project! 🚀 Feeling accomplished and ready for the next challenge.",
        "Beautiful morning coffee and some deep thoughts about technology's future ☕️",
        "Working on something exciting. Can't wait to share it with everyone soon! 🎉",
        "Loved this book recommendation from a friend. Anyone else read it? 📚",
        "Amazing sunset from my balcony today. Nature never fails to inspire 🌅"
    };

    private static string[] articleTitles = new string[]
    {
        "Breaking: Revolutionary AI Technology Unveiled",
        "Climate Scientists Make Groundbreaking Discovery",
        "Tech Giants Announce Major Collaboration",
        "New Study Reveals Surprising Health Benefits",
        "Space Mission Returns with Fascinating Data"
    };

    private static string[] articleDescriptions = new string[]
    {
        "Researchers have developed a new method that could change everything we know...",
        "The implications of this discovery could reshape our understanding of...",
        "Industry experts are calling this the most significant development in...",
        "Scientists from leading universities collaborated to uncover...",
        "This breakthrough opens up possibilities that were previously unimaginable..."
    };

    public List<NewsItem> GetNewsFeed(int count)
    {
        var items = new List<NewsItem>();

        for (int i = 0; i < count; i++)
        {
            index++;
            var newsType = GetRandomNewsType();

            var author = GetRandomAuthor();
            var item = new NewsItem
            {
                Id = index,
                Type = newsType,
                AuthorName = author.name,
                AuthorAvatarUrl = author.avatarUrl,
                PublishedAt = DateTime.Now.AddMinutes(-random.Next(1, 1440)) // Random time within last day
            };

            ConfigureItemByType(item);
            items.Add(item);
        }

        return items;
    }

    private void ConfigureItemByType(NewsItem item)
    {
        switch (item.Type)
        {
            case NewsType.Text:
                item.Content = postTexts[random.Next(postTexts.Length)];
                break;

            case NewsType.Image:
                item.Content = postTexts[random.Next(postTexts.Length)];
                // High-quality random images from Picsum
                item.ImageUrl = $"https://picsum.photos/seed/{index}/600/400";
                break;

            case NewsType.Video:
                item.Title = "Amazing Video Content";
                item.Content = "Check out this incredible footage!";
                // Video thumbnail from Picsum
                item.VideoUrl = $"https://picsum.photos/seed/{index}/600/400";
                break;

            case NewsType.Article:
                item.Title = articleTitles[random.Next(articleTitles.Length)];
                item.Content = articleDescriptions[random.Next(articleDescriptions.Length)];
                item.ImageUrl = $"https://picsum.photos/seed/{index}/400/300";
                item.ArticleUrl = "https://example.com/article";
                break;

            case NewsType.Ad:
                item.Title = "Special Offer - Don't Miss Out!";
                item.Content = "Limited time offer on premium features";
                item.ImageUrl = $"https://picsum.photos/seed/{index}/600/200";
                break;
        }

        // Random engagement numbers
        item.LikesCount = random.Next(0, 1000);
        item.CommentsCount = random.Next(0, 150);
    }

    private NewsType GetRandomNewsType()
    {
        // Weighted distribution for realistic feed
        var typeWeights = new (NewsType type, int weight)[]
        {
            (NewsType.Text, 30),    // 30% text posts
            (NewsType.Image, 40),   // 40% image posts
            (NewsType.Video, 15),   // 15% videos
            (NewsType.Article, 10), // 10% articles
            (NewsType.Ad, 5)        // 5% ads
        };

        var totalWeight = typeWeights.Sum(x => x.weight);
        var randomValue = random.Next(totalWeight);

        var currentWeight = 0;
        foreach (var (type, weight) in typeWeights)
        {
            currentWeight += weight;
            if (randomValue < currentWeight)
                return type;
        }

        return NewsType.Text;
    }

    private (string name, string avatarUrl) GetRandomAuthor()
    {
        return authors[random.Next(authors.Length)];
    }
}
```

### 6. Feed Implementation with Real Data and Image Preloading

> **Spacing Strategy**: Stack spacing is 0 because cell padding provides the spacing between items
> **Recycling**: RecyclingTemplate="Enabled" with MeasureItemsStrategy="MeasureFirst" for optimal performance

```xml
<!-- MainPage.xaml excerpt -->
<draw:SkiaScroll
    x:Name="NewsScroll"
    Orientation="Vertical"
    RefreshCommand="{Binding RefreshCommand}"
    LoadMoreCommand="{Binding LoadMoreCommand}"
    RefreshEnabled="True"
    HorizontalOptions="Fill"
    ResetScrollPositionOnContentSizeChanged="False"
    VerticalOptions="Fill">

    <draw:SkiaScroll.Header>
        <draw:SkiaLayer HeightRequest="40" UseCache="Image">
            <draw:SkiaRichLabel
                Text="DrawnUI News Feed Tutorial"
                HorizontalOptions="Center" VerticalOptions="Center" />
        </draw:SkiaLayer>
    </draw:SkiaScroll.Header>

    <draw:SkiaScroll.Footer>
        <draw:SkiaLayer HeightRequest="50" />
    </draw:SkiaScroll.Footer>

    <!-- Dynamic height cells using SkiaLayout with ItemTemplate -->
    <draw:SkiaLayout
        x:Name="NewsStack"
        Type="Column"
        ItemsSource="{Binding NewsItems}"
        RecyclingTemplate="Enabled"
        MeasureItemsStrategy="MeasureFirst"
        Spacing="0"
        HorizontalOptions="Fill">

        <draw:SkiaLayout.ItemTemplate>
            <DataTemplate>
                <views:NewsCell />
            </DataTemplate>
        </draw:SkiaLayout.ItemTemplate>

    </draw:SkiaLayout>

</draw:SkiaScroll>
```

```csharp
using System.Diagnostics;
using System.Windows.Input;
using AppoMobi.Specials;
using Sandbox.Models;
using Sandbox.Services;

namespace Sandbox.ViewModels;

public class NewsViewModel : BaseViewModel
{
    private readonly NewsDataProvider _dataProvider;
    private CancellationTokenSource _preloadCancellation;

    public NewsViewModel()
    {
        _dataProvider = new NewsDataProvider();
        NewsItems = new ObservableRangeCollection<NewsItem>();

        RefreshCommand = new Command(async () => await RefreshFeed());
        LoadMoreCommand = new Command(async () => await LoadMore());

        // Load initial data
        _ = RefreshFeed();
    }

    public ObservableRangeCollection<NewsItem> NewsItems { get; }

    public ICommand RefreshCommand { get; }
    public ICommand LoadMoreCommand { get; }

    private async Task RefreshFeed()
    {
        if (IsBusy) return;

        IsBusy = true;

        try
        {
            // Cancel previous preloading
            _preloadCancellation?.Cancel();

            Debug.WriteLine($"Loading news feed !!!");

            // Generate fresh content
            var newItems = _dataProvider.GetNewsFeed(20);

            // Preload images in background (DrawnUI's SkiaImageManager)
            _preloadCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            _ = PreloadImages(newItems, _preloadCancellation.Token);

            // Update UI - Replace all items for refresh
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.ReplaceRange(newItems);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error refreshing feed: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task LoadMore()
    {
        if (IsBusy) return;

        IsBusy = true;

        try
        {
            Debug.WriteLine("Loading more items !!!");
            var newItems = _dataProvider.GetNewsFeed(15);

            // Preload new images
            _preloadCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            _ = PreloadImages(newItems, _preloadCancellation.Token);

            // Add new items to the end of the collection
            MainThread.BeginInvokeOnMainThread(() =>
            {
                NewsItems.AddRange(newItems);
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading more: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task PreloadImages(List<NewsItem> items, CancellationToken cancellationToken)
    {
        try
        {
            var imageUrls = new List<string>();

            // Add content images
            imageUrls.AddRange(items
                .Where(x => !string.IsNullOrEmpty(x.ImageUrl))
                .Select(x => x.ImageUrl));

            // Add avatar images
            imageUrls.AddRange(items
                .Where(x => !string.IsNullOrEmpty(x.AuthorAvatarUrl))
                .Select(x => x.AuthorAvatarUrl));

            // Use DrawnUI's image manager for efficient preloading
            await SkiaImageManager.Instance.PreloadImages(imageUrls, _preloadCancellation);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error preloading images: {ex.Message}");
        }
    }
}

// MainPage.xaml.cs
public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = new NewsViewModel();
    }
}
```

## Key Advantages

### 1. **Perfect Recycling with Smart Caching**
- Single cell type = maximum recycling efficiency
- `UseCache="Image"` for uneven row heights (this example)
- `UseCache="ImageDoubleBuffered"` would be even faster for same-height cells
- Strategic cache placement: `UseCache="Operations"` for text, `UseCache="Image"` for complex layouts

### 2. **Uneven Row Heights Challenge Solved**
This tutorial demonstrates the challenging case of **uneven row heights**:

```csharp
// Different content types = different heights:
// - Text post = ~120dp
// - Image post = ~320dp
// - Video post = ~320dp
// - Article preview = ~180dp
// - Ad content = ~250dp
// All calculated automatically by DrawnUI's layout system
```

> **💡 Want to see equal row heights?** Check out the demo engine app (referenced in these docs) where we have another example with equal-sized cells that you can swipe to reveal controls behind them. That example uses `UseCache="ImageDoubleBuffered"` for even smoother performance!

### 3. **Real Internet Images with Proper Preloading**
- **Avatars**: RandomUser.me API (100x100px professional portraits)
- **Content**: Picsum Photos API (high-quality random images)
- **Preloading**: Both avatars AND content images preloaded for smooth scrolling
- **Caching**: DrawnUI's SkiaImageManager handles efficient image caching

### 4. **Shadow Performance Optimization**
- Shadows are cached inside cell padding to avoid performance issues
- Wrapper layouts with `UseCache="Image"` contain shadow effects
- Cell padding creates space for shadows to render properly

### 5. **Spacing Strategy**
- Stack `Spacing="0"` because cell padding provides item spacing
- Cell `Padding="16,6,16,10"` acts as general spacing between items
- This approach caches shadows inside the padding area

### 6. **LoadMore Implementation**
- **Refresh**: Uses `ReplaceRange()` to replace all items
- **LoadMore**: Uses `AddRange()` to append new items (proper infinite scroll)
- **ObservableRangeCollection**: Efficient bulk operations without individual notifications

### 7. **Smart Text Rendering**
- **SkiaLabel**: Fast rendering when your font family supports all symbols
- **SkiaRichLabel**: Auto-finds installed fonts for Unicode symbols (emojis, special characters)
- **No more "???"**: SkiaRichLabel prevents missing symbol fallbacks by finding the right fonts
- **Customizable fallbacks**: SkiaLabel lets you customize fallback symbols if needed

### 8. **Icons & Animations (Bonus!)**
We kept it simple with emoji for this tutorial, but DrawnUI gives you amazing options:
- **SkiaSvg**: Crisp vector icons for like, share, play buttons that scale perfectly
- **SkiaLottie**: Animated icons that bring your UI to life (think animated hearts, loading spinners)
- **Performance**: Both render with hardware acceleration and cache beautifully

## Conclusion: Just Draw What You Want

DrawnUI gives you the freedom to **just draw what you need**. This tutorial demonstrates a challenging real-world scenario:

### ✅ **What We Accomplished**
- **One universal cell** handling 5 different content types with uneven heights
- **Real internet images** from RandomUser.me (avatars) and Picsum Photos (content)
- **Proper image preloading** for both avatars and content images
- **Smart caching strategy** using `UseCache="Image"` for uneven heights
- **Shadow performance optimization** cached inside cell padding
- **Proper LoadMore** implementation with `AddRange()` vs `ReplaceRange()`
- **Strategic spacing** using cell padding instead of stack spacing

### 🎯 **Performance Optimizations**
- **Caching**: `UseCache="Image"` for layouts, `UseCache="Operations"` for text
- **Shadows**: Wrapped in cached containers to avoid performance issues
- **Spacing**: Stack spacing = 0, cell padding provides item spacing
- **Images**: Both avatars and content preloaded with DrawnUI's SkiaImageManager

### 🚀 **The DrawnUI Advantage**
Adding a new content type? Simply add an enum value and a configuration method. No new templates, no complex selectors, no performance compromises.

The result? A smooth, efficient news feed that handles the challenging case of uneven row heights while loading real images from the internet. **Just draw what you want!** 🎨