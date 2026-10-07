using ContextMenuManager.Methods;
using ContextMenuManager.Properties;
using System;
using System.Windows.Controls;

namespace ContextMenuManager.Views
{
    public partial class AboutAppView : UserControl
    {
        private const string GitHubUrl = "https://github.com/NeoLi-Coder/ContextMenuManager";
        private const string UpstreamUrl = "https://github.com/Jack251970/ContextMenuManager";

        public AboutAppView()
        {
            InitializeComponent();
            LogoImage.Source = AppResources.Logo.ToBitmapSource();
            RefreshContent();
        }

        public void RefreshContent()
        {
            AppNameText.Text = AppString.General.AppName;
            GitHubLinkText.Content = $"{AppString.About.GitHub ?? "GitHub"}: {GitHubUrl}";
            GitHubLinkText.NavigateUri = new Uri(GitHubUrl);
            UpstreamLinkText.Content = $"{(AppConfig.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? "上游项目" : "Upstream")}: {UpstreamUrl}";
            UpstreamLinkText.NavigateUri = new Uri(UpstreamUrl);
            LicenseText.Text = $"{AppString.About.License ?? "License"}: GPL License";
            CheckUpdateButton.Content = AppString.About.CheckUpdate ?? "Check Update";
        }

        private void CheckUpdateButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            Updater.Update(true);
        }
    }
}
