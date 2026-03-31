using Microsoft.Maui.Controls;
using Microsoft.Maui.Dispatching;
using System;
using System.Threading.Tasks;

namespace CMS
{
    internal partial class DiscussionsPage : ContentPage
    {
        IDispatcherTimer _timer;
        DateTime _lastChecked = DateTime.UtcNow;
        private object _gitHubService;

        public DiscussionsPage()
        {
            InitializeComponent();

            // Настраиваем таймер (например, проверять раз в 60 секунд)
            _timer = Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(60);
            _timer.Tick += async (s, e) => await CheckForUpdates();
            _timer.Start();
        }

        private void InitializeComponent()
        {
            throw new NotImplementedException();
        }

        private async Task CheckForUpdates()
        {
            // 1. Запрашиваем последние обсуждения через Octokit (GraphQL)
            // 2. Сравниваем дату создания самого свежего сообщения с _lastChecked

            var latestDiscussions = await _gitHubService.GetNewDiscussions(_lastChecked);

            if (latestDiscussions.Any())
            {
                // Обновляем UI
                foreach (var item in latestDiscussions)
                {
                    DiscussionsCollection.Insert(0, item);
                }

                // Обновляем метку времени последнего чека
                _lastChecked = DateTime.UtcNow;

                // Показываем локальное уведомление (Toast)
                await DisplayAlert("Новое в Discussions", "Появились свежие темы!", "Ок");
            }
        }
    }

}
