using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Yusupov41
{
    /// <summary>
    /// Логика взаимодействия для OrderWindow.xaml
    /// </summary>
    public partial class OrderWindow : Window
    {
        List<OrderProduct> selectedOrderProducts = new List<OrderProduct>();
        List<Product> selectedProducts = new List<Product>();
        private Order currentOrder = new Order();
        private OrderProduct currentOrderProduct = new OrderProduct();
        public OrderWindow(List<OrderProduct> selectedOrderProducts, List<Product> selectedProducts, string FIO, User user)
        {
            InitializeComponent();
            currentOrder.OrderClientID = user?.UserID;
            var currentPickups = Yusupov41Entities.GetContext().PickUpPoint.ToList();
            PickupCombo.ItemsSource = currentPickups;

            ClientTB.Text = FIO;
            TBOrderID.Text = selectedOrderProducts.First().OrderID.ToString();

            ProductListView.ItemsSource = selectedProducts;
            foreach (Product p in selectedProducts)
            {
                p.ProductQuantityInStock = 1;
                foreach (OrderProduct q in selectedOrderProducts)
                {
                    if (p.ProductArticleNumber == q.ProductArticleNumber)
                        p.ProductQuantityInStock = q.OrderProductCount;
                }
            }

            this.selectedOrderProducts = selectedOrderProducts;
            this.selectedProducts = selectedProducts;
            OrderDP.Text = DateTime.Now.ToString();
            SetDeliveryDate();
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            if (PickupCombo.SelectedItem == null)
            {
                MessageBox.Show("Выберите пункт выдачи!");
                return;
            }

            if (selectedOrderProducts == null || !selectedOrderProducts.Any())
            {
                MessageBox.Show("Добавьте товары в заказ!");
                return;
            }

            try
            {
                // ЗАПОЛНЯЕМ currentOrder данными из формы
                currentOrder.OrderDate = DateTime.Now;
                currentOrder.OrderDeliveryDate = DateTime.Parse(DeliveryDateTextBox.Text);
                currentOrder.OrderPickupPoint = (PickupCombo.SelectedItem as PickUpPoint).PickUpPointID;
                currentOrder.OrderStatus = "Новый";

                // Генерируем код заказа
                var lastOrder = Yusupov41Entities.GetContext().Order
                    .OrderByDescending(o => o.OrderCode)
                    .FirstOrDefault();
                currentOrder.OrderCode = lastOrder != null ? lastOrder.OrderCode + 1 : 1;

                // Добавляем связь с товарами
                currentOrder.OrderProduct = selectedOrderProducts;

                // Сохраняем заказ в БД
                Yusupov41Entities.GetContext().Order.Add(currentOrder);
                Yusupov41Entities.GetContext().SaveChanges();

                MessageBox.Show("Заказ успешно сохранен!");
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении заказа: {ex.Message}");
            }
        }
        private void SetDeliveryDate()
        {
            if (selectedOrderProducts == null || !selectedOrderProducts.Any())
            {
                DeliveryDateTextBox.Text = DateTime.Now.AddDays(6).ToString("dd.MM.yyyy");
                return;
            }

            // Подсчитываем общее количество товаров (сумма всех Quantity)
            int totalProductCount = selectedOrderProducts.Sum(op => op.OrderProductCount);

            int deliveryDays;
            if (totalProductCount < 3)
            {
                deliveryDays = 6; // Менее 3 товаров - 6 дней
            }
            else
            {
                deliveryDays = 3; // 3 и более товаров - 3 дня
            }

            DateTime deliveryDate = DateTime.Now.AddDays(deliveryDays);
            DeliveryDateTextBox.Text = deliveryDate.ToString("dd.MM.yyyy");
            OrderDP.Text = DateTime.Now.ToString("dd.MM.yyyy");
        }

        private void BtnPlus_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button).DataContext as Product;
            if (product != null)
            {
                product.ProductQuantityInStock++;

                var selectedOP = selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == product.ProductArticleNumber);
                if (selectedOP != null)
                {
                    selectedOP.OrderProductCount++;
                }

                ProductListView.Items.Refresh();
                SetDeliveryDate();
            }
        }

        private void BtnMinus_Click(object sender, RoutedEventArgs e)
        {
            var product = (sender as Button).DataContext as Product;
            if (product != null && product.ProductQuantityInStock > 1)
            {
                product.ProductQuantityInStock--;

                var selectedOP = selectedOrderProducts.FirstOrDefault(p => p.ProductArticleNumber == product.ProductArticleNumber);
                if (selectedOP != null)
                {
                    selectedOP.OrderProductCount--;
                }

                ProductListView.Items.Refresh();
                SetDeliveryDate();
            }
        }
    }
}
