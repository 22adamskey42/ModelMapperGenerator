using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using ModelMapperGenerator.Constants;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using System.IO;

namespace ModelMapperGenerator.UnitTests.Infrastructure
{
    internal static class AssertHelpers
    {
        public static void AssertGeneratorRunResultsCaching(GeneratorDriverRunResult firstRunResult, GeneratorDriverRunResult secondRunResult)
        {
            GeneratorRunResult firstResults = Assert.Single(firstRunResult.Results);
            GeneratorRunResult secondResults = Assert.Single(secondRunResult.Results);

            Assert.Equal(2, firstRunResult.GeneratedTrees.Length);
            Assert.Equal(2, secondRunResult.GeneratedTrees.Length);

            ImmutableArray<IncrementalGeneratorRunStep> firstTrackedOutputValue = Assert.Single(firstResults.TrackedOutputSteps).Value;
            ImmutableArray<IncrementalGeneratorRunStep> secondTrackedOutputValue = Assert.Single(secondResults.TrackedOutputSteps).Value;

            ImmutableArray<(object Value, IncrementalStepRunReason Reason)> firstOutputValue = Assert.Single(firstTrackedOutputValue).Outputs;
            ImmutableArray<(object Value, IncrementalStepRunReason Reason)> secondOutputValue = Assert.Single(secondTrackedOutputValue).Outputs;

            IncrementalStepRunReason firstReason = Assert.Single(firstOutputValue).Reason;
            IncrementalStepRunReason secondReason = Assert.Single(secondOutputValue).Reason;

            Assert.Equal(IncrementalStepRunReason.New, firstReason);
            Assert.Equal(IncrementalStepRunReason.Cached, secondReason);

            Assert.Equal(14, firstResults.TrackedSteps.Count);
            Assert.Equal(14, secondResults.TrackedSteps.Count);

            string[] knownSteps = [
                TrackingStepsConstants.GetTargetTypesStep,
                TrackingStepsConstants.CollectTargetDescriptors,
            ];

            List<KeyValuePair<string, ImmutableArray<IncrementalGeneratorRunStep>>> trackedSteps = firstResults.TrackedSteps.Where(x => knownSteps.Contains(x.Key)).ToList();
            foreach (KeyValuePair<string, ImmutableArray<IncrementalGeneratorRunStep>> step in trackedSteps)
            {
                KeyValuePair<string, ImmutableArray<IncrementalGeneratorRunStep>> correspondingSecondRunStep = secondResults.TrackedSteps.FirstOrDefault(x => x.Key == step.Key);
                Assert.False(correspondingSecondRunStep.Equals(default(KeyValuePair<string, ImmutableArray<IncrementalGeneratorRunStep>>)));
                foreach (IncrementalGeneratorRunStep stepValue in step.Value)
                {
                    IncrementalGeneratorRunStep? secondStepValue = correspondingSecondRunStep.Value.FirstOrDefault(x => x.Name == stepValue.Name);
                    Assert.NotNull(secondStepValue);

                    foreach ((object FirstStepValue, IncrementalStepRunReason FirstStepReason) in stepValue.Outputs)
                    {
                        (object SecondStepValue, IncrementalStepRunReason SecondStepReason) secondStepOutput = secondStepValue.Outputs.FirstOrDefault(x => x.Value.Equals(x.Value));
                        Assert.False(secondStepOutput.Equals(default((object Value, IncrementalStepRunReason Reason))));

                        Assert.Equal(IncrementalStepRunReason.New, FirstStepReason);
                        Assert.Equal(IncrementalStepRunReason.Cached, secondStepOutput.SecondStepReason);
                    }
                }
            }
        }

        public static void AssertOutputCompilation(ref ImmutableArray<Diagnostic> diagnostics, Compilation outputCompilation, int expectedSyntaxTreeCount, bool verifyDiagsEmpty = true)
        {
            Assert.True(diagnostics.IsEmpty);
            Assert.Equal(expectedSyntaxTreeCount, outputCompilation.SyntaxTrees.Count());
            if (verifyDiagsEmpty)
            {
                ImmutableArray<Diagnostic> diags = outputCompilation.GetDiagnostics();
                Assert.True(diags.IsEmpty);
            }
        }

        public static async Task AssertGeneratedCodeAsync(string expectedFileName, string expectedFileContent, Compilation outputCompilation)
        {
            SyntaxTree? syntaxTree = outputCompilation.SyntaxTrees.FirstOrDefault(x => x.FilePath.EndsWith(expectedFileName));
            Assert.NotNull(syntaxTree);
            string fileName = Path.GetFileName(syntaxTree.FilePath);
            Assert.Equal(expectedFileName, fileName);

            SourceText sourceText = await syntaxTree.GetTextAsync();
            string stringOfSourceText = sourceText.ToString();
            Assert.Equal(expectedFileContent, stringOfSourceText);
        }
    }
}
