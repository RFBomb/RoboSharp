using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RoboSharp.Benchmarks
{
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net462, baseline: true)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net48)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net50)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class ProgressDataRegex
    {
        [Params("0.00%", "10%", "100%", "New File  \t\t       4\t4_Bytes.txt", "New Dir          0\tC:\\Repos\\SubFolder_1\\SubFolder_1.1\\SubFolder_1.2\\")]
        public string Data { get; set; }

        [Benchmark] 
        public bool IsMatch() => RoboCommand.Process_OutputProgressDataRegex().IsMatch(Data);
    }


    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net462, baseline: true)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net48)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net50)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class OutputDirectoryRegex
    {
        [Params("New Dir          4\tC:\\", "Existing Dir          4\tC:\\Repos\\SubFolder_1\\SubFolder_1.1\\SubFolder_1.2\\")]
        public string Data;

        [Benchmark]
        public void Match() => RoboCommand.Process_OutputDirectoryDataRegex().Match(Data);
    }

    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net462, baseline: true)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net48)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net50)]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    public class DefaultConfigurations_ErrorRegex
    {
        const string errorFormat = "2024/05/30 09:26:02 {0} 32 (0x00000020) Copying File C:\\Repos\\RoboSharp\\RoboSharpUnitTesting\\bin\\Release\\net8.0\\TEST_FILES\\STANDARD\\4_Bytes.txt";

        private const string _NoMatch = "  Started : Thursday, May 30, 2024 9:26:02 AM";
        private string _error;
        private RoboSharpConfiguration _configuration;

        [GlobalSetup(Targets = new string[] { nameof(IsNoMatch), nameof(NoMatch), nameof(US_ErrorRegex_IsMatch), nameof(US_ErrorRegex_Match) })]
        public void Setup_US()
        {
            _configuration = new DefaultConfigurations.RoboSharpConfig_EN();
            _error = string.Format(errorFormat, _configuration.ErrorToken);
        }

        [GlobalSetup(Targets = new string[] { nameof(DE_ErrorRegex_IsMatch), nameof(DE_ErrorRegex_Match) })]
        public void Setup_DE()

        {
            _configuration = new DefaultConfigurations.RoboSharpConfig_DE();
            _error = string.Format(errorFormat, _configuration.ErrorToken);
        }

        [Benchmark]
        [BenchmarkCategory("IsMatch")]
        public bool IsNoMatch() => _configuration.ErrorTokenRegex.IsMatch(_NoMatch);

        [Benchmark]
        [BenchmarkCategory("IsMatch")]
        public bool NoMatch() => _configuration.ErrorTokenRegex.Match(_NoMatch).Success;

        [Benchmark]
        [BenchmarkCategory("en-US", "IsMatch")]
        public void US_ErrorRegex_IsMatch() => _configuration.ErrorTokenRegex.IsMatch(_error);

        [Benchmark]
        [BenchmarkCategory("en-US", "Match")]
        public void US_ErrorRegex_Match() => _configuration.ErrorTokenRegex.Match(_error);

        [Benchmark]
        [BenchmarkCategory("DE", "IsMatch")]
        public void DE_ErrorRegex_IsMatch() => _configuration.ErrorTokenRegex.IsMatch(_error);

        [Benchmark]
        [BenchmarkCategory("DE", "Match")] 
        public void DE_ErrorRegex_Match() => _configuration.ErrorTokenRegex.Match(_error);

    }
}
