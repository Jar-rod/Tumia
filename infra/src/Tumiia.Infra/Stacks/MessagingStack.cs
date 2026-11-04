using Amazon.CDK;
using Amazon.CDK.AWS.CloudWatch;
using Amazon.CDK.AWS.CloudWatch.Actions;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace Tumiia.Infra.Stacks;

/// <summary>
/// The document-analysis queue and its dead-letter queue (DLQ). A message that fails three times moves to the
/// DLQ, and anything landing there emails the alert address.
/// </summary>
public sealed class MessagingStack : Stack
{
    /// <summary>The API sends one message per uploaded document here; the analysis worker reads them.</summary>
    public Queue Queue { get; }

    /// <summary>Messages that failed three times. Should always be empty.</summary>
    public Queue DeadLetterQueue { get; }

    /// <summary>Alarm notifications, sent by email to the alert address.</summary>
    public Topic AlertsTopic { get; }

    /// <summary>Creates the queues, the alerts topic and the dead-letter alarm.</summary>
    public MessagingStack(Construct scope, string id, IStackProps props, InfraSettings settings, ResourceNames names)
        : base(scope, id, props)
    {
        DeadLetterQueue = new Queue(this, "DeadLetterQueue", new QueueProps
        {
            QueueName = names.DeadLetterQueueName,
            RetentionPeriod = Duration.Days(14),
            Encryption = QueueEncryption.SQS_MANAGED,
            EnforceSSL = true,
        });

        // Five minutes gives the worker time to OCR a multi-page scan before the message reappears.
        Queue = new Queue(this, "Queue", new QueueProps
        {
            QueueName = names.QueueName,
            VisibilityTimeout = Duration.Minutes(5),
            DeadLetterQueue = new DeadLetterQueue { Queue = DeadLetterQueue, MaxReceiveCount = 3 },
            Encryption = QueueEncryption.SQS_MANAGED,
            EnforceSSL = true,
        });

        AlertsTopic = new Topic(this, "AlertsTopic", new TopicProps { TopicName = $"{settings.EnvironmentName}-tumiia-alerts" });
        AlertsTopic.AddSubscription(new EmailSubscription(settings.AlertEmail));

        AddDeadLetterAlarm(names);
    }

    /// <summary>Raises the alarm as soon as one message is waiting in the dead-letter queue.</summary>
    private void AddDeadLetterAlarm(ResourceNames names)
    {
        var alarm = new Alarm(this, "DeadLetterQueueNotEmpty", new AlarmProps
        {
            AlarmName = $"{names.DeadLetterQueueName}-not-empty",
            AlarmDescription = "A document failed analysis three times and is waiting in the dead-letter queue.",
            Metric = DeadLetterQueue.MetricApproximateNumberOfMessagesVisible(new MetricOptions
            {
                Period = Duration.Minutes(1),
                Statistic = "Maximum",
            }),
            Threshold = 0,
            ComparisonOperator = ComparisonOperator.GREATER_THAN_THRESHOLD,
            EvaluationPeriods = 1,
            TreatMissingData = TreatMissingData.NOT_BREACHING,
        });

        alarm.AddAlarmAction(new SnsAction(AlertsTopic));
    }
}
