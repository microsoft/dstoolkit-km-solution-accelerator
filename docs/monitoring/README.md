![banner](../media/banner.png)

# Monitoring

Our solution accelerator components including the UI are monitored with Azure Application Insights. 

![](media/ApplicationInsights.png)

## Azure Application Insights

Application Insights is a feature of Azure Monitor that provides extensible application performance management (APM) and monitoring for live web apps. Developers and DevOps professionals can use Application Insights to:

Automatically detect performance anomalies.
Help diagnose issues by using powerful analytics tools.
See what users actually do with apps.
Help continuously improve app performance and usability.
Application Insights:

Supports a wide variety of platforms, including .NET, Node.js, Java, and Python.
Works for apps hosted on-premises, hybrid, or on any public cloud.
Integrates with DevOps processes.
Has connection points to many development tools.
Can monitor and analyze telemetry from mobile apps by integrating with Visual Studio App Center.

Learn more on [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)

## Microsoft Clarity 

[Microsoft Clarity](https://www.bing.com/ck/a?!&&p=ce74ec63d33fab18JmltdHM9MTY2MTQyNTI0MyZpZ3VpZD0zNzJlNzYzMi1kZDdmLTRkN2YtYjE0OC01YzhlZmY2NmVjMzQmaW5zaWQ9NTE4MQ&ptn=3&hsh=3&fclid=29c0a873-2465-11ed-8d8d-bc03f515005d&u=a1aHR0cHM6Ly9jbGFyaXR5Lm1pY3Jvc29mdC5jb20v&ntb=1) - Free Heatmaps & Session Recordings.

See what your users want—with Clarity Clarity is a free, easy-to-use tool that captures how real people actually use your site. Setup is easy and you'll start getting data in minutes. GDPR & CCPA ready No sampling. Built on open source and it's free!

![](media/MicrosoftClarity.png)

Our accelerator UI provided a ready-to-use Clarity integration as long as you provide a Project Id in the environment configuration file. 

```json
    "clarityProjectId":"<your project id>",
```

