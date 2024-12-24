using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Text.Json.Serialization;
namespace frontend
{


    public class Productcreate
    {
        [JsonPropertyName("product_id")]
        public string ProductID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("sku")]
        public string SKU { get; set; }

        [JsonPropertyName("category_name")]
        public string CategoryName { get; set; }

        [JsonPropertyName("quantity")]
        public string Quantity { get; set; }

        [JsonPropertyName("unit_price")]
        public string UnitPrice { get; set; }

        [JsonPropertyName("barcode")]
        public string Barcode { get; set; }

        [JsonPropertyName("created_at")]
        public string CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public string UpdatedAt { get; set; }
    }



    public partial class CreateProductWindow : Window
    {
        public CreateProductWindow()
        {
            InitializeComponent();
        }

        private async void createButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var product = new Productcreate
                {
                    Name = nameBox.Text,
                    SKU = skuBox.Text,
                    CategoryName = categoryBox.Text,
                    Quantity = quantityBox.Text,
                    UnitPrice = priceBox.Text,
                    Barcode = barcodeBox.Text,
                };

                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("http://127.0.0.1:3500");
                    var json = JsonSerializer.Serialize(product);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await client.PostAsync("/products/create", content);

                    var responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Response Status: {response.StatusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");

                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Product created successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        DialogResult = true;
                    }
                    else
                    {
                        MessageBox.Show($"Failed to create product: {response.StatusCode}\n{responseContent}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
