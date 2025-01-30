﻿namespace DrawnUi.Maui.Draw
{
    public partial class SkiaScroll
    {
        // tuning background cells measurement
        protected virtual int IncrementalMeasureAheadCount { get; set; } = 1;
        protected virtual int IncrementalMeasureBatchSize { get; set; } = 1;
        protected virtual double MeasurementTriggerDistance { get; set; } = 0;//500.0;
        private bool _incrementalMeasurementInProgress = false;


        // Check if we need more items
        protected bool? CheckForIncrementalMeasurementTrigger()
        {
            if (_incrementalMeasurementInProgress)
                return null;

            if (Content is SkiaLayout layout && layout.IsTemplated
                                             && layout.MeasureItemsStrategy == MeasuringStrategy.MeasureVisible
                                             && layout.LastMeasuredIndex < layout.ItemsSource.Count)
            {
                var measuredEnd = layout.GetMeasuredContentEnd();

                double currentOffset = Orientation == ScrollOrientation.Vertical
                    ? -ViewportOffsetY
                    : -ViewportOffsetX;

                if (measuredEnd - currentOffset < MeasurementTriggerDistance)
                {
                    TriggerIncrementalMeasurement(layout);
                    return true;
                }

            }

            return false;
        }

        private SemaphoreSlim _lockAdditionalMeasurement = new(1);

        private bool? _lastCheck;

        protected void TriggerIncrementalMeasurement(SkiaLayout layout)
        {
            _incrementalMeasurementInProgress = true;

            async Task DoMeasure()
            {
                await _lockAdditionalMeasurement.WaitAsync();

                // Measure next batch of items + ahead
                int measuredCount = layout.MeasureAdditionalItems(IncrementalMeasureBatchSize, IncrementalMeasureAheadCount, RenderingScale);

                _lockAdditionalMeasurement.Release();

                _incrementalMeasurementInProgress = false;

                Update();

                if (_lastCheck == null || _lastCheck.Value)
                {
                    _lastCheck = CheckForIncrementalMeasurementTrigger();
                }
            }

            Task.Run(DoMeasure);
        }

    }
}
