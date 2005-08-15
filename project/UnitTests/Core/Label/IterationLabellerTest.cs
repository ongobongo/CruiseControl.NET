using System;
using Exortech.NetReflector;
using NMock;
using NUnit.Framework;
using ThoughtWorks.CruiseControl.Core;
using ThoughtWorks.CruiseControl.Core.Label;
using ThoughtWorks.CruiseControl.Core.Util;

namespace ThoughtWorks.CruiseControl.UnitTests.Core.Label
{
	[TestFixture]
	public class IterationLabellerTest : CustomAssertion
	{
		private IterationLabeller labeller;
		private DateTime releaseStartDate = new DateTime(2005, 01, 01, 00, 00, 00, 00);
		private IMock dateTimeMock;

		[SetUp]
		public void SetUp()
		{
			dateTimeMock = new DynamicMock(typeof(DateTimeProvider));
			dateTimeMock.SetupResult("Today", new DateTime(2005, 7, 20, 0, 0, 0, 0));
			labeller = new IterationLabeller((DateTimeProvider) dateTimeMock.MockInstance);
			labeller.ReleaseStartDate = releaseStartDate;
		}

		[Test]
		public void PopulateFromConfiguration()
		{
			DateTime releaseStartDate = new DateTime(2005, 1, 1);
			string xml = string.Format(@"<iterationlabeller duration=""1"" releaseStartDate=""{0}"" prefix=""foo"" separator=""-"" />", releaseStartDate);
			labeller = (IterationLabeller) NetReflector.Read(xml);
			Assert.AreEqual(1, labeller.Duration);
			Assert.AreEqual("foo", labeller.LabelPrefix);
			Assert.AreEqual("-", labeller.Separator);
			Assert.AreEqual(releaseStartDate, labeller.ReleaseStartDate);
		}

		[Test]
		public void GenerateIncrementedLabel()
		{
			Assert.AreEqual("14.36", labeller.Generate(IntegrationResultMother.CreateSuccessful("14.35")));
		}

		[Test]
		public void GenerateWithNullLabel()
		{
			IntegrationResult result = IntegrationResultMother.CreateSuccessful((string)null);
			Assert.AreEqual("14.1", labeller.Generate(result));
		}

		[Test]
		public void GenerateAfterLastBuildFailed()
		{
			Assert.AreEqual("14.23", labeller.Generate(IntegrationResultMother.CreateFailed("14.23")));
		}

		[Test]
		public void GeneratePrefixedLabelWithNullResultLabel()
		{
			IntegrationResult result = IntegrationResultMother.CreateSuccessful();
			result.Label = null;
			labeller.LabelPrefix = "Sample";
			Assert.AreEqual("Sample.14.1", labeller.Generate(result));
		}

		[Test]
		public void GeneratePrefixedLabelOnSuccessAndPreviousLabel()
		{
			labeller.LabelPrefix = "Sample";
			Assert.AreEqual("Sample.14.24", labeller.Generate(IntegrationResultMother.CreateSuccessful("Sample.14.23")));
		}

		[Test]
		public void GeneratePrefixedLabelOnFailureAndPreviousLabel()
		{
			labeller.LabelPrefix = "Sample";
			Assert.AreEqual("Sample.14.23", labeller.Generate(IntegrationResultMother.CreateFailed("Sample.14.23")));
		}

		[Test]
		public void GeneratePrefixedLabelOnSuccessAndPreviousLabelWithDifferentPrefix()
		{
			labeller.LabelPrefix = "Sample";
			Assert.AreEqual("Sample.14.24", labeller.Generate(IntegrationResultMother.CreateSuccessful("SomethingElse.14.23")));
		}

		[Test]
		public void IncrementPrefixedLabelWithNumericPrefix()
		{
			labeller.LabelPrefix = "R3SX";
			Assert.AreEqual("R3SX.14.24", labeller.Generate(IntegrationResultMother.CreateSuccessful("R3SX.14.23")));
		}

		[Test]
		public void IncrementPrefixedLabelWithNumericSeperatorSeperatedPrefix()
		{
			labeller.LabelPrefix = "1.0";
			Assert.AreEqual("1.0.14.24", labeller.Generate(IntegrationResultMother.CreateSuccessful("1.0.14.23")));
		}
		
		[Test]
		public void WhenTheBuildIsPerformedDuringANewIterationTheIterationNumberIsUpdatedAndTheLabelReset()
		{
			// Set the release start date needs to be 15 iterations ago
			// from today.  So take today's date and remove 15 weeks and a couple more days.
			dateTimeMock.SetupResult("Today", DateTime.Today);
			labeller.ReleaseStartDate = DateTime.Today.AddDays(- (15 * 7 + 2) );
			
			// one week iterations
			labeller.Duration = 1;
			Assert.AreEqual("15.1", labeller.Generate(IntegrationResultMother.CreateSuccessful("14.35")));						
		}

		[Test]
		public void WhenTheLabelIsUpdatedDueToANewIterationThePrefixRemains()
		{
			// Set the release start date needs to be 15 iterations ago
			// from today.  So take today's date and remove 15 weeks and a couple more days.
			dateTimeMock.SetupResult("Today", DateTime.Today);
			labeller.ReleaseStartDate = DateTime.Today.AddDays(- (15 * 7 + 2) );
			
			// one week iterations
			labeller.Duration = 1;

			labeller.LabelPrefix = "R3SX";
			Assert.AreEqual("R3SX.15.1", labeller.Generate(IntegrationResultMother.CreateSuccessful("R3SX.14.23")));
		}
	}
}