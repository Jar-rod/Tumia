using Amazon.CDK.Assertions;

namespace Tumiia.Infra.Tests;

[Collection(CdkCollection.Name)]
public class MessagingTests(SynthesizedStacks stacks)
{
    [Fact]
    public void Queue_MovesAMessageToTheDeadLetterQueueAfterThreeFailures()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Messaging);

        template.HasResourceProperties("AWS::SQS::Queue", Match.ObjectLike(new Dictionary<string, object>
        {
            ["QueueName"] = "prod-document-analysis",
            ["VisibilityTimeout"] = 300,
            ["RedrivePolicy"] = Match.ObjectLike(new Dictionary<string, object> { ["maxReceiveCount"] = 3 }),
        }));
    }

    [Fact]
    public void DeadLetterQueue_HasAnAlarmThatEmailsAsSoonAsOneMessageArrives()
    {
        var template = Template.FromStack(stacks.BeforeFirstDeploy.Messaging);

        template.HasResourceProperties("AWS::CloudWatch::Alarm", Match.ObjectLike(new Dictionary<string, object>
        {
            ["MetricName"] = "ApproximateNumberOfMessagesVisible",
            ["Threshold"] = 0,
            ["ComparisonOperator"] = "GreaterThanThreshold",
            ["AlarmActions"] = Match.AnyValue(),
        }));
        template.HasResourceProperties("AWS::SNS::Subscription", new Dictionary<string, object>
        {
            ["Protocol"] = "email",
            ["Endpoint"] = "alerts@example.co.za",
            ["TopicArn"] = Match.AnyValue(),
        });
    }
}
