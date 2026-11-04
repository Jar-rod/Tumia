using Amazon.CDK;
using Tumiia.Infra;

// Entry point for "npx cdk ...". Reads the settings from CDK context and builds every stack.
var app = new App();
var settings = InfraSettings.FromContext(app.Node);
TumiiaStacks.Create(app, settings);
app.Synth();
