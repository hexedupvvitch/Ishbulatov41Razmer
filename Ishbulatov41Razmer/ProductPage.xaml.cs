using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Ishbulatov41Razmer
{
    public partial class ProductPage : Page
    {
        public class CartItem
        {
            public string ProductArticleNumber { get; set; }
            public int Quantity { get; set; }
            public Product Product { get; set; }
        }

        private List<Product> allProducts;
        private List<CartItem> cartItems = new List<CartItem>();

        public ProductPage(User user)
        {
            InitializeComponent();

            // Отображение ФИО и роли
            FIOTB.Text = user.UserSurname + " " + user.UserName + " " + user.UserPatronymic;

            switch (user.UserRole)
            {
                case 1:
                    RoleTB.Text = "Клиент";
                    break;
                case 2:
                    RoleTB.Text = "Менеджер";
                    break;
                case 3:
                    RoleTB.Text = "Администратор";
                    break;
                default:
                    RoleTB.Text = "Гость";
                    break;
            }

            // Загрузка всех товаров из БД
            allProducts = Ishbulatov41Entities.GetContext().Product.ToList();
            ProductListView.ItemsSource = allProducts;

            ComboType.SelectedIndex = 0;
            UpdateStatus();
        }

        private void UpdateStatus()
        {
            int displayedCount = ProductListView.Items.Count;
            int totalCount = allProducts.Count;
            Status.Text = displayedCount + " из " + totalCount;
        }

        private void UpdateProduct()
        {
            var currentProduct = allProducts.AsEnumerable();

            // Фильтрация по скидке
            if (ComboType.SelectedIndex == 0)
            {
                // Все диапазоны - без фильтра
            }
            else if (ComboType.SelectedIndex == 1)
            {
                // от 0 до 9,99%
                currentProduct = currentProduct.Where(p => p.ProductDiscountAmount >= 0 && p.ProductDiscountAmount <= 9);
            }
            else if (ComboType.SelectedIndex == 2)
            {
                // от 10 до 14,99%
                currentProduct = currentProduct.Where(p => p.ProductDiscountAmount >= 10 && p.ProductDiscountAmount <= 14);
            }
            else if (ComboType.SelectedIndex == 3)
            {
                // от 15% и более
                currentProduct = currentProduct.Where(p => p.ProductDiscountAmount >= 15);
            }

            // Поиск по наименованию
            if (!string.IsNullOrWhiteSpace(TBoxSearch.Text))
            {
                currentProduct = currentProduct.Where(p => p.ProductName.ToLower().Contains(TBoxSearch.Text.ToLower()));
            }

            // Сортировка
            if (RButtonDown.IsChecked == true)
            {
                currentProduct = currentProduct.OrderByDescending(p => p.ProductCost);
            }
            else if (RButtonUp.IsChecked == true)
            {
                currentProduct = currentProduct.OrderBy(p => p.ProductCost);
            }

            ProductListView.ItemsSource = currentProduct.ToList();
            UpdateStatus();
        }

        private void TBoxSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateProduct();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboType.SelectedIndex >= 0)
            {
                UpdateProduct();
            }
        }

        private void RButtonDown_Checked(object sender, RoutedEventArgs e)
        {
            if (allProducts != null)
            {
                UpdateProduct();
            }
        }

        private void RButtonUp_Checked(object sender, RoutedEventArgs e)
        {
            if (allProducts != null)
            {
                UpdateProduct();
            }
        }

        // Добавление товара в корзину через контекстное меню
        private void AddToOrderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedProduct = ProductListView.SelectedItem as Product;
            if (selectedProduct == null) return;

            var existingCartItem = cartItems.FirstOrDefault(ci => ci.ProductArticleNumber == selectedProduct.ProductArticleNumber);

            if (existingCartItem == null)
            {
                var newCartItem = new CartItem
                {
                    ProductArticleNumber = selectedProduct.ProductArticleNumber,
                    Quantity = 1,
                    Product = selectedProduct
                };
                cartItems.Add(newCartItem);

                MessageBox.Show("Товар '" + selectedProduct.ProductName + "' добавлен в заказ",
                              "Успешно", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                existingCartItem.Quantity++;
                MessageBox.Show("Количество товара '" + selectedProduct.ProductName + "' увеличено до " + existingCartItem.Quantity,
                              "Обновлено", MessageBoxButton.OK, MessageBoxImage.Information);
            }

            UpdateOrderButton();
            ProductListView.SelectedIndex = -1;
        }

        private void UpdateOrderButton()
        {
            if (cartItems.Count > 0)
            {
                ViewOrderBtn.Visibility = Visibility.Visible;
                int totalQuantity = cartItems.Sum(ci => ci.Quantity);
                ViewOrderBtn.Content = "Корзина (" + totalQuantity + ")";
            }
            else
            {
                ViewOrderBtn.Visibility = Visibility.Collapsed;
            }
        }

        private void ViewOrderBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenOrderWindow();
        }

        private void OpenOrderWindow()
        {
            try
            {
                if (cartItems.Count == 0)
                {
                    MessageBox.Show("Добавьте товары в заказ!", "Информация",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                string clientFIO = FIOTB.Text;
                var orderWindow = new OrderWindow1(cartItems, clientFIO);
                orderWindow.Closed += OrderWindow_Closed;
                orderWindow.ShowDialog();

                // После закрытия окна (любого) обновляем кнопку
                UpdateOrderButton();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OrderWindow_Closed(object sender, EventArgs e)
        {
            var orderWindow = sender as OrderWindow1;
            if (orderWindow != null)
            {
                if (orderWindow.DialogResult == true)
                {
                    // Заказ оформлен - очищаем корзину
                    cartItems.Clear();
                    UpdateOrderButton();
                }
                else
                {
               
                    UpdateOrderButton();
                }
            }
        }
    }
}