using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Ishbulatov41Razmer
{
    public partial class AuthPage : Page
    {
        private string currentCaptcha;
        private int failedAttempts = 0;
        private DateTime? blockUntil = null;
        private DispatcherTimer blockTimer;

        public AuthPage()
        {
            InitializeComponent();
            InitializeBlockTimer();
            UpdateLoginButtonState();

            // Скрываем капчу при запуске
            CaptchaPanel.Visibility = Visibility.Collapsed;
            CaptchaInputPanel.Visibility = Visibility.Collapsed;
        }

        // Инициализация таймера блокировки
        private void InitializeBlockTimer()
        {
            blockTimer = new DispatcherTimer();
            blockTimer.Interval = TimeSpan.FromSeconds(1);
            blockTimer.Tick += BlockTimer_Tick;
        }

        // Генерация и отображение капчи
        private void GenerateCaptcha()
        {
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            Random random = new Random();
            currentCaptcha = "";

            // Генерируем 4 случайных символа
            for (int i = 0; i < 4; i++)
            {
                currentCaptcha += chars[random.Next(chars.Length)];
            }

            // Отображаем символы в TextBlock'ах
            if (currentCaptcha.Length >= 4)
            {
                captchaOneWord.Text = currentCaptcha[0].ToString();
                captchaTwoWord.Text = currentCaptcha[1].ToString();
                captchaThreeWord.Text = currentCaptcha[2].ToString();
                captchaFourWord.Text = currentCaptcha[3].ToString();
            }
        }

        // Показать капчу
        private void ShowCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Visible;
            CaptchaInputPanel.Visibility = Visibility.Visible;
            GenerateCaptcha();
            CaptchaTextBox.Text = "";
            CaptchaTextBox.Focus();
        }

        // Скрыть капчу
        private void HideCaptcha()
        {
            CaptchaPanel.Visibility = Visibility.Collapsed;
            CaptchaInputPanel.Visibility = Visibility.Collapsed;
            CaptchaTextBox.Text = "";
            failedAttempts = 0;
        }

        // Проверка капчи
        private bool ValidateCaptcha(string userInput)
        {
            return userInput == currentCaptcha;
        }

        // Блокировка системы на 10 секунд
        private void BlockSystem()
        {
            blockUntil = DateTime.Now.AddSeconds(10);
            blockTimer.Start();
            UpdateLoginButtonState();
        }

        // Обновление состояния кнопки входа
        private void UpdateLoginButtonState()
        {
            if (blockUntil.HasValue && DateTime.Now < blockUntil.Value)
            {
                // Система заблокирована
                TimeSpan remaining = blockUntil.Value - DateTime.Now;
                LogIn.Content = "Заблокировано (" + remaining.Seconds + "с)";
                LogIn.IsEnabled = false;
                Guest.IsEnabled = false;
                CaptchaTextBox.IsEnabled = false;
            }
            else
            {
                // Система разблокирована
                LogIn.Content = "Войти";
                LogIn.IsEnabled = true;
                Guest.IsEnabled = true;
                CaptchaTextBox.IsEnabled = true;
                blockUntil = null;
                blockTimer.Stop();
            }
        }

        // Обработчик таймера блокировки
        private void BlockTimer_Tick(object sender, EventArgs e)
        {
            UpdateLoginButtonState();
        }

        // Вход как гость
        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            User guestUser = new User
            {
                UserID = 0,
                UserLogin = "guest",
                UserPassword = "",
                UserName = "Гость",
                UserPatronymic = "",
                UserSurname = "",
                UserRole = 0
            };

            // Очищаем поля
            TBoxlog.Text = "";
            TBoxpass.Password = "";
            HideCaptcha();

            // Переходим на страницу товаров
            Manager.MainFrame.Navigate(new ProductPage(guestUser));
        }

        // Вход по логину и паролю
        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            string login = TBoxlog.Text.Trim();
            string password = TBoxpass.Password.Trim();

            // Проверка на пустые поля
            if (string.IsNullOrEmpty(login) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!", "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Поиск пользователя в БД
            User user = Ishbulatov41Entities.GetContext().User
                .FirstOrDefault(p => p.UserLogin == login && p.UserPassword == password);

            if (user != null)
            {
                // Успешный вход
                TBoxlog.Text = "";
                TBoxpass.Password = "";
                HideCaptcha();
                failedAttempts = 0;
                Manager.MainFrame.Navigate(new ProductPage(user));
            }
            else
            {
                // Неверный логин или пароль
                failedAttempts++;

                if (failedAttempts == 1)
                {
                    // Первая неудачная попытка — показываем капчу
                    MessageBox.Show("Введены неверные данные. Теперь необходимо ввести капчу.",
                                  "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    ShowCaptcha();
                }
                else if (failedAttempts >= 2)
                {
                    // Вторая и последующие попытки — проверяем капчу
                    string captchaInput = CaptchaTextBox.Text.Trim();

                    if (string.IsNullOrEmpty(captchaInput) || !ValidateCaptcha(captchaInput))
                    {
                        // Неверная капча — блокировка на 10 секунд
                        MessageBox.Show("Неверная капча! Вход заблокирован на 10 секунд.",
                                      "Блокировка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        BlockSystem();
                        GenerateCaptcha();
                        CaptchaTextBox.Text = "";
                    }
                    else
                    {
                        // Капча верная, но логин/пароль неверные
                        MessageBox.Show("Введены неверные данные!",
                                      "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        GenerateCaptcha();
                        CaptchaTextBox.Text = "";
                    }
                }

                // Очищаем поле пароля для повторного ввода
                TBoxpass.Password = "";
            }
        }
    }
}