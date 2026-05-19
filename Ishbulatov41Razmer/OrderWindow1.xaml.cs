using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Ishbulatov41Razmer
{
    public partial class OrderWindow1 : Window
    {
        private List<ProductPage.CartItem> cartItems;
        private string clientFIO;
        private decimal totalSum = 0;
        private decimal totalDiscount = 0;

        public OrderWindow1(List<ProductPage.CartItem> cartItems, string clientFIO)
        {
            InitializeComponent();

            this.cartItems = cartItems;
            this.clientFIO = clientFIO;

            // Дата заказа - сегодня
            DateOrder.SelectedDate = DateTime.Today;

            // ФИО клиента
            FIOTB.Text = clientFIO;

            // Загружаем пункты выдачи
            var pickUpPoints = Ishbulatov41Entities.GetContext().PickUpPoint.ToList();
            ComboPickUpPoint.ItemsSource = pickUpPoints;

            // Генерируем новый номер заказа
            GenerateOrderNumber();

            // Загружаем товары в ListView
            LoadOrderProducts();

            // Устанавливаем дату доставки
            SetDateDelivery();

            // Подсчитываем сумму
            UpdateOrderSummary();
        }

        private void GenerateOrderNumber()
        {
            try
            {
                using (var context = new Ishbulatov41Entities())
                {
                    var maxCode = context.Order.Max(o => (int?)o.OrderCode) ?? 0;
                    NUMBERTB.Text = (maxCode + 1).ToString();
                }
            }
            catch
            {
                NUMBERTB.Text = "1";
            }
        }

        private void LoadOrderProducts()
        {
            var displayProducts = new List<Product>();
            using (var context = new Ishbulatov41Entities())
            {
                foreach (var cartItem in cartItems)
                {
                    var product = context.Product
                        .FirstOrDefault(p => p.ProductArticleNumber == cartItem.ProductArticleNumber);
                    if (product != null)
                    {
                        product.ProductQuantityInStock = cartItem.Quantity;
                        displayProducts.Add(product);
                    }
                }
            }
            ShoeListView.ItemsSource = displayProducts;
        }

        private void SetDateDelivery()
        {
            DateTime today = DateTime.Now;
            bool allProductsHaveEnoughStock = true;

            if (cartItems.Count == 0)
            {
                allProductsHaveEnoughStock = false;
            }
            else
            {
                using (var context = new Ishbulatov41Entities())
                {
                    foreach (var cartItem in cartItems)
                    {
                        var product = context.Product
                            .FirstOrDefault(p => p.ProductArticleNumber == cartItem.ProductArticleNumber);

                        if (product == null || product.ProductQuantityInStock < 3)
                        {
                            allProductsHaveEnoughStock = false;
                            break;
                        }
                    }
                }
            }

            if (allProductsHaveEnoughStock)
            {
                DateDelivery.SelectedDate = today.AddDays(3);
                DateDelivery.ToolTip = "Срок доставки: 3 дня (все товары в наличии ≥ 3 шт.)";
            }
            else
            {
                DateDelivery.SelectedDate = today.AddDays(6);
                DateDelivery.ToolTip = "Срок доставки: 6 дней (некоторые товары менее 3 шт. или отсутствуют)";
            }
        }

        private decimal CalculateTotal()
        {
            totalSum = 0;
            totalDiscount = 0;

            foreach (var cartItem in cartItems)
            {
                var product = cartItem.Product;
                if (product != null)
                {
                    decimal price = product.ProductCost;
                    decimal discountPercent = product.ProductDiscountAmount;
                    decimal discountAmount = price * discountPercent / 100;
                    decimal finalPrice = price - discountAmount;

                    totalSum += finalPrice * cartItem.Quantity;
                    totalDiscount += discountAmount * cartItem.Quantity;
                }
            }

            return totalSum;
        }

        private void UpdateOrderSummary()
            {
                CalculateTotal();

                Cost.Text = $"{totalSum:N2} руб.";
                DiscountTB.Text = $"{totalDiscount:N2} руб.";
            }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            var product = button.Tag as Product;
            if (product == null) return;

            var cartItem = cartItems.FirstOrDefault(ci => ci.ProductArticleNumber == product.ProductArticleNumber);
            if (cartItem != null)
            {
                cartItem.Quantity++;
                product.ProductQuantityInStock = cartItem.Quantity;
            }

            SetDateDelivery();
            UpdateOrderSummary();
            ShoeListView.Items.Refresh();
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            var product = button.Tag as Product;
            if (product == null) return;

            var cartItem = cartItems.FirstOrDefault(ci => ci.ProductArticleNumber == product.ProductArticleNumber);
            if (cartItem != null && cartItem.Quantity > 0)
            {
                cartItem.Quantity--;

                if (cartItem.Quantity == 0)
                {
                    cartItems.Remove(cartItem);
                    LoadOrderProducts();
                }
                else
                {
                    product.ProductQuantityInStock = cartItem.Quantity;
                }
            }

            SetDateDelivery();
            UpdateOrderSummary();
            ShoeListView.Items.Refresh();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button?.Tag == null) return;

            var product = button.Tag as Product;
            if (product == null) return;

            var cartItem = cartItems.FirstOrDefault(ci => ci.ProductArticleNumber == product.ProductArticleNumber);
            if (cartItem != null)
            {
                cartItems.Remove(cartItem);
            }

            LoadOrderProducts();
            SetDateDelivery();
            UpdateOrderSummary();
            ShoeListView.Items.Refresh();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cartItems.Count == 0)
                {
                    MessageBox.Show("Добавьте хотя бы один товар в заказ!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedPickup = ComboPickUpPoint.SelectedItem as PickUpPoint;
                if (selectedPickup == null)
                {
                    MessageBox.Show("Выберите пункт выдачи!", "Ошибка",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(NUMBERTB.Text, out int orderCode))
                {
                    orderCode = 1;
                }

                using (var context = new Ishbulatov41Entities())
                {
                    var newOrder = new Order
                    {
                        OrderDate = DateOrder.SelectedDate ?? DateTime.Today,
                        OrderDeliveryDate = DateDelivery.SelectedDate ?? DateTime.Today.AddDays(3),
                        OrderPickupPoint = selectedPickup.PickUpPointId,
                        OrderCode = orderCode,
                        OrderStatus = "Новый",
                        OrderClientId = null
                    };

                    context.Order.Add(newOrder);
                    context.SaveChanges();

                    foreach (var cartItem in cartItems)
                    {
                        var orderProduct = new OrderProduct
                        {
                            OrderID = newOrder.OrderID,
                            ProductArticleNumber = cartItem.ProductArticleNumber,
                            OrderProductAmount = cartItem.Quantity
                        };
                        context.OrderProduct.Add(orderProduct);
                    }
                    context.SaveChanges();
                }

                MessageBox.Show(string.Format("Заказ №{0} успешно оформлен!\nСумма: {1:N2} руб.\nСкидка: {2:N2} руб.\nДата доставки: {3:dd.MM.yyyy}",
                    NUMBERTB.Text, totalSum, totalDiscount, DateDelivery.SelectedDate),
                    "Заказ оформлен", MessageBoxButton.OK, MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка при сохранении заказа: " + ex.Message, "Ошибка",
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void ComboPickUpPoint_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ShoeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }
    }
}