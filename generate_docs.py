import os

pages = [
    ("Overview", "overview", "Introduction and Overview of HealthyCron."),
    ("Configuration", "configuration", "Learn how to configure your monitors."),
    ("RunningWithDocker", "running-with-docker", "Guide on running HealthyCron inside a Docker container."),
    ("ReliabilityTips", "reliability-tips", "Best practices to ensure your monitoring is reliable."),
    ("CronSyntaxCheatsheet", "cron-syntax-cheatsheet", "A quick reference for writing cron expressions."),
    ("ComparedToSentry", "compared-to-sentry", "How HealthyCron compares to Sentry for monitoring."),
    ("ComparedToCronitor", "compared-to-cronitor", "How HealthyCron compares to Cronitor."),
    ("ShellScripts", "shell-scripts", "Integrating HealthyCron with Shell Scripts."),
    ("Arduino", "arduino", "Pinging HealthyCron from IoT devices like Arduino."),
    ("NetworkRouters", "network-routers", "Monitoring network routers (MikroTik, pfSense, etc)."),
    ("CSharp", "csharp", "Integrating HealthyCron in C# .NET applications."),
    ("Email", "email", "Pinging via Email."),
    ("GitHubActions", "github-actions", "Monitoring CI/CD pipelines in GitHub Actions."),
    ("Go", "go", "Integrating in Go applications."),
    ("Javascript", "javascript", "Integrating in JavaScript/Node.js apps."),
    ("Php", "php", "Integrating in PHP applications."),
    ("PowerShell", "powershell", "Pinging from Windows PowerShell."),
    ("Python", "python", "Integrating in Python applications."),
    ("Ruby", "ruby", "Integrating in Ruby applications.")
]

views_dir = "/Users/amanpandey/Desktop/Repos/healthycron/Views/Docs"

template = """@{
    ViewData["Title"] = "%s";
    Layout = "_DocsLayout";
    ViewData["Breadcrumb"] = "Docs / %s";
}

<div class="mb-10">
    <h1 class="text-3xl font-bold text-white mb-4 tracking-tight">%s</h1>
    <p class="text-hc-muted text-lg">%s</p>
</div>

<div class="prose max-w-none text-hc-muted">
    <p>This is a placeholder page for the %s documentation. Detailed content for this section will be added here.</p>
    
    <div class="callout callout-info">
        <div class="callout-title">
            <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z"></path></svg>
            Under Construction
        </div>
        <div class="callout-body">
            This documentation page is currently being drafted. Connect your application and start pinging to explore everything HealthyCron has to offer.
        </div>
    </div>
</div>
"""

controller_methods = ""
for name, slug, desc in pages:
    file_path = os.path.join(views_dir, f"{name}.cshtml")
    with open(file_path, "w") as f:
        f.write(template % (name, name, name, desc, name))
    
    controller_methods += f"""
        [HttpGet("{slug}")]
        public IActionResult {name}()
        {{
            return View();
        }}
"""

print("Generated files in", views_dir)
print("\nController Methods to add:")
print(controller_methods)
