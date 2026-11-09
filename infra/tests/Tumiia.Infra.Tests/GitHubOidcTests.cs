namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class GitHubOidcTests(SynthesizedStacks stacks)
{
    [Fact]
    public void DeployRole_CanOnlyBeAssumedFromThisRepositorysMainBranchOrProdEnvironment()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.GitHubOidc);
        var role = SynthesizedStacks.Resources(template, "AWS::IAM::Role").Single();

        var condition = role["Properties"]!["AssumeRolePolicyDocument"]!["Statement"]![0]!["Condition"]!["StringEquals"]!;
        var allowedSubjects = condition["token.actions.githubusercontent.com:sub"]!.AsArray().Select(subject => subject!.GetValue<string>()).ToList();

        condition["token.actions.githubusercontent.com:aud"]!.GetValue<string>().ShouldBe("sts.amazonaws.com");
        allowedSubjects.ShouldBe(
        [
            $"repo:{SynthesizedStacks.Repo}:ref:refs/heads/main",
            $"repo:{SynthesizedStacks.Repo}:environment:prod",
        ], ignoreOrder: true);
    }

    [Fact]
    public void DeployRole_CanOnlyStopAndStartThisEnvironmentsDatabase()
    {
        var template = SynthesizedStacks.Json(stacks.BeforeFirstDeploy.GitHubOidc);
        var statements = SynthesizedStacks.Resources(template, "AWS::IAM::Policy")
            .SelectMany(policy => policy["Properties"]!["PolicyDocument"]!["Statement"]!.AsArray())
            .Select(statement => statement!)
            .ToList();

        var stopStart = statements.Single(statement => statement["Sid"]?.GetValue<string>() == "StopAndStartDatabase");
        var tags = stopStart["Condition"]!["StringEquals"]!;

        stopStart["Action"]!.AsArray().Select(action => action!.GetValue<string>()).ShouldBe(["rds:StopDBInstance", "rds:StartDBInstance"], ignoreOrder: true);
        tags["aws:ResourceTag/project"]!.GetValue<string>().ShouldBe("tumiia");
        tags["aws:ResourceTag/env"]!.GetValue<string>().ShouldBe("prod");
    }
}
