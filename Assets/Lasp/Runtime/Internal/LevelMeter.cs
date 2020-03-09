using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Lasp
{
    //
    // Audio level meter class with low/mid/high filter bank
    //
    sealed class LevelMeter
    {
        #region Public properties

        public float4 GetLevel(int channel) => _levels[channel];
        public float SampleRate { get; set; }

        #endregion

        #region Internal state

        float4 [] _levels;
        MultibandFilter [] _filters;

        #endregion

        #region Public methods

        public LevelMeter(int channels)
        {
            _levels = new float4 [channels];
            _filters = new MultibandFilter [channels];
        }

        public void ProcessAudioData(ReadOnlySpan<float> input)
        {
            if (input.Length == 0) return;

            using (var tempInput = NativeArrayUtil.NewTempJob(input))
            using (var tempLevels = NativeArrayUtil.NewTempJob(_levels))
            using (var tempFilters = NativeArrayUtil.NewTempJob(_filters))
            {
                // Run the job on the main thread.
                new FilterRmsJob
                {
                    Input    = tempInput,
                    Filters  = tempFilters,
                    FilterFc = 960.0f / SampleRate,
                    FilterQ  = 0.15f,
                    Output   = tempLevels
                }
                  .Run(_levels.Length);

                // Retrieve the output from the temporary native arrays.
                tempLevels.CopyTo(_levels);
                tempFilters.CopyTo(_filters);
            }
        }

        #endregion

        #region Signal processing job

        [Unity.Burst.BurstCompile]
        struct FilterRmsJob : IJobFor
        {
            [ReadOnly] public NativeSlice<float> Input;

            public NativeArray<MultibandFilter> Filters; // read/write
            public float FilterFc, FilterQ;

            [WriteOnly] public NativeArray<float4> Output;

            public void Execute(int i)
            {
                var channels = Output.Length;
                var filter = Filters[i];

                // Filter parameter update
                filter.SetParameter(FilterFc, FilterQ);

                // Squared sum
                var sum = float4.zero;
                for (var offs = i; offs < Input.Length; offs += channels)
                {
                    var vf = filter.FeedSample(Input[offs]);
                    sum += vf * vf;
                }

                // Root mean square
                var rms = math.sqrt(sum / (Input.Length / channels));

                // Output
                Output[i] = rms;
                Filters[i] = filter;
            }
        }

        #endregion
    }
}
