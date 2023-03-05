using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Security.Cryptography;

namespace RFord.Benchmark.GuidVsIntConcat
{
    internal class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<GuidVsIntConcat>();
        }
    }

    // so if we do in-method loops of a set count, the results appear in line
    // with what could reasonably be expected, and what rough (i.e. not rigidly
    // controlled) experimentation shows.  however, if we set it to a single in-
    // method iteration, but with a similar number of TEST iterations, it shows
    // different results.
    // e.g.
    //  1000 in-method iterations, 1 test iteration => B, A, C are the rankings, where A-C are three different methods
    //  1 in-method iteration, 1000 test iterations => A, C, B are the rankings
    /*
    |                         Method | count |       Mean |     Error |    StdDev | Ratio | RatioSD |
    |------------------------------- |------ |-----------:|----------:|----------:|------:|--------:|
    |                  GuidGenerator |  1000 |  83.108 us | 1.7793 us | 5.1051 us |  1.00 |    0.00 |
    |       ConcatenatedIntGenerator |  1000 |   8.961 us | 0.1792 us | 0.1992 us |  0.11 |    0.01 |
    | SecureConcatenatedIntGenerator |  1000 | 101.659 us | 2.0310 us | 5.0201 us |  1.23 |    0.10 |
    */
    public class GuidVsIntConcat
    {
        public IEnumerable<int> Count => new[] { 1000 };

#pragma warning disable CS8618 // Non-nullable variable must contain a non-null value when exiting constructor. Consider declaring it as nullable.  GlobalSetupAttribute can't be applied to a ctor, so we suppress this warning.  The BenchmarkRunner will let us know at runtime if the test tries to access a null or undefined value.  :D
        private Random _insecureRandom;
        private RandomNumberGenerator _secureRandom;
        private byte[] _buffer;
#pragma warning restore CS8618

        [GlobalSetup]
        public void GlobalSetup()
        {
            _insecureRandom = new Random();
            _secureRandom = RandomNumberGenerator.Create();
            _buffer = new byte[16];
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _secureRandom.Dispose();
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(Count))]
        public void GuidGenerator(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Guid result = Guid.NewGuid();
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void ConcatenatedIntGenerator(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _insecureRandom.NextBytes(_buffer);
                Guid result = new Guid(_buffer);
            }
        }

        // so this one confused me a bit, because it was roughly on par with the
        // straight `Guid.NewGuid()` call, which was unexpected.  then I started
        // digging into the `Guid` struct sourcecode @ source.dot.net and it
        // turns out that the default `.NewGuid()` actually does use a secure
        // random data generator under the surface, which - to me - explains the
        // similarity in run time!
        [Benchmark]
        [ArgumentsSource(nameof(Count))]
        public void SecureConcatenatedIntGenerator(int count)
        {
            for (int i = 0; i < count; i++)
            {
                _secureRandom.GetBytes(_buffer);
                Guid result = new Guid(_buffer);
            }
        }
    }
}