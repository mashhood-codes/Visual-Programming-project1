using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace frontend
{
    public partial class updatewindow : Window
    {
        private List<Product> products;

        public updatewindow(List<Product> existingProducts)
        {
            InitializeComponent();
            products = existingProducts;

            comboxproductid.ItemsSource = products.Select(p => p.ProductID);

            comboxcolumnnames.ItemsSource = new[] { "Name", "SKU", "CategoryName", "Quantity", "UnitPrice", "Barcode" };
        }

        private async void update_button_Click(object sender, RoutedEventArgs e)
        {
            if (comboxproductid.SelectedItem == null || comboxcolumnnames.SelectedItem == null || string.IsNullOrWhiteSpace(comboboxnewvalue.Text))
            {
                MessageBox.Show("Please fill in all fields", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var selectedProduct = products.First(p => p.ProductID == comboxproductid.SelectedItem);
                var updateObject = new Product
                {
                    ProductID = selectedProduct.ProductID,
                    Name = comboxcolumnnames.SelectedItem.ToString() == "Name" ? comboboxnewvalue.Text : "",
                    SKU = comboxcolumnnames.SelectedItem.ToString() == "SKU" ? comboboxnewvalue.Text : "",
                    CategoryName = comboxcolumnnames.SelectedItem.ToString() == "CategoryName" ? comboboxnewvalue.Text : "",
                    Quantity = comboxcolumnnames.SelectedItem.ToString() == "Quantity" ? comboboxnewvalue.Text : "",
                    UnitPrice = comboxcolumnnames.SelectedItem.ToString() == "UnitPrice" ? comboboxnewvalue.Text : "",
                    Barcode = comboxcolumnnames.SelectedItem.ToString() == "Barcode" ? comboboxnewvalue.Text : "",
                    CreatedAt = "",
                    UpdatedAt = ""
                };

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:3500");
                    var json = JsonSerializer.Serialize(updateObject);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync($"/products/update/{updateObject.ProductID}", content);

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Product updated successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show($"Failed to update product: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
