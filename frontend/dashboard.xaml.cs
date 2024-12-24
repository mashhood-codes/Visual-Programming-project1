using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json.Serialization;
namespace frontend
{

    public class DashboardData : INotifyPropertyChanged
    {
        private float _totalSales;
        private int _totalProducts;
        private int _lowStockAlert;

        public event PropertyChangedEventHandler PropertyChanged;

        public float totalSalesnum
        {
            get { return _totalSales; }
            set
            {
                if (_totalSales != value)
                {
                    _totalSales = value;
                    OnPropertyChanged(nameof(totalSalesnum));
                }
            }
        }

        public int totalProductsnum
        {
            get { return _totalProducts; }
            set
            {
                if (_totalProducts != value)
                {
                    _totalProducts = value;
                    OnPropertyChanged(nameof(totalProductsnum));
                }
            }
        }

        public int lowstocknum
        {
            get { return _lowStockAlert; }
            set
            {
                if (_lowStockAlert != value)
                {
                    _lowStockAlert = value;
                    OnPropertyChanged(nameof(lowstocknum));
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public HttpClient requester { get; set; }

        public DashboardData()
        {
            requester = new HttpClient();
            requester.BaseAddress = new Uri("http://127.0.0.1:3500");

            _ = LoadDashboardDataAsync();
        }

        private async System.Threading.Tasks.Task LoadDashboardDataAsync()
        {
            try
            {
                HttpResponseMessage response = await requester.GetAsync("/dashboard");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true 
                    };

                    var dashboardDetails = JsonSerializer.Deserialize<DashboardData>(json, options);

                    if (dashboardDetails != null)
                    {
                        totalProductsnum = dashboardDetails.totalProductsnum;
                        totalSalesnum = dashboardDetails.totalSalesnum;
                        lowstocknum = dashboardDetails.lowstocknum;
                    }
                    else
                    {
                        MessageBox.Show("Received empty data from the server.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load data: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public class Product
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
    public class OrderManagementData
    {

        public int? purchase_order_id { get; set; }
        public string supplier_name { get; set; }
        public string purchase_order_date { get; set; }
        public string purchase_order_status { get; set; }
        public double purchase_total_amount { get; set; }

        public int? sales_order_id { get; set; }
        public string customer_name { get; set; }
        public string sales_order_date { get; set; }
        public string sales_order_status { get; set; }
        public double sales_total_amount { get; set; }

        public int? movement_id { get; set; }
        public string movement_type { get; set; }
        public int? movement_quantity { get; set; }
        public string movement_date { get; set; }
        public string movement_description { get; set; }
    }

    public class OrderResponse
    {
        public string status { get; set; }
        public List<OrderManagementData> data { get; set; }
    }


    public class AuditLog
    {
        [JsonPropertyName("logid")]
        public int LogId { get; set; }

        [JsonPropertyName("username")]
        public string Username { get; set; }

        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("action")]
        public string Action { get; set; }

        [JsonPropertyName("tableaffected")]
        public string TableAffected { get; set; }

        [JsonPropertyName("action_time")]
        public string ActionTime { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }
    }

    public class AuditResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("data")]
        public List<AuditLog> Data { get; set; }
    }


    public class SearchWindow : Window
    {
        private readonly HttpClient _client;
        private readonly DataGrid _productsGrid;
        private TextBox searchBox;

        public SearchWindow(HttpClient client, DataGrid productsGrid)
        {
            _client = client;
            _productsGrid = productsGrid;

            Title = "Search Products";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };

            var stackPanel = new StackPanel();

            var searchLabel = new TextBlock
            {
                Text = "Enter Search Term:",
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold
            };
            stackPanel.Children.Add(searchLabel);

            searchBox = new TextBox
            {
                Padding = new Thickness(5),
                Margin = new Thickness(0, 0, 0, 20),
                Height = 30
            };
            stackPanel.Children.Add(searchBox);

            var executeButton = new Button
            {
                Content = "Execute Search",
                Height = 35,
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(20, 5, 20, 5),
                Background = new SolidColorBrush(Colors.DodgerBlue),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.SemiBold
            };
            executeButton.Click += ExecuteSearch_Click;
            stackPanel.Children.Add(executeButton);

            grid.Children.Add(stackPanel);
            Content = grid;
        }

        private async void ExecuteSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
            {
                MessageBox.Show("Please enter a search term", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var response = await _client.GetAsync($"/products/search?term={Uri.EscapeDataString(searchBox.Text)}");

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var queryResult = JsonSerializer.Deserialize<QueryResult>(json, options);

                    if (queryResult?.Rows != null && queryResult.Columns != null)
                    {
                        var products = MapRowsToProducts(queryResult.Rows, queryResult.Columns);
                        _productsGrid.ItemsSource = products;
                        Close();
                        MessageBox.Show($"Found {products.Count} products matching your search.", "Search Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("No products found matching your search criteria.", "No Results", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show($"Search failed: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private List<Product> MapRowsToProducts(List<List<string>> rows, List<string> columns)
        {
            var products = new List<Product>();
            foreach (var row in rows)
            {
                products.Add(new Product
                {
                    ProductID = GetValueOrDefault(row, columns, "ProductID"),
                    Name = GetValueOrDefault(row, columns, "Name"),
                    SKU = GetValueOrDefault(row, columns, "SKU"),
                    CategoryName = GetValueOrDefault(row, columns, "CategoryName"),
                    Quantity = GetValueOrDefault(row, columns, "Quantity") ?? "N/A",
                    UnitPrice = GetValueOrDefault(row, columns, "UnitPrice") ?? "N/A",
                    Barcode = GetValueOrDefault(row, columns, "Barcode") ?? "N/A",
                    CreatedAt = GetValueOrDefault(row, columns, "CreatedAt") ?? "N/A",
                    UpdatedAt = GetValueOrDefault(row, columns, "UpdatedAt") ?? "N/A"
                });
            }
            return products;
        }

        private string GetValueOrDefault(List<string> row, List<string> columns, string columnName)
        {
            int index = columns.IndexOf(columnName);
            return index >= 0 && index < row.Count ? row[index] : null;
        }
    }






    public class ProductId
    {
        public int product_id { get; set; }
    }
    public class DeleteWindow : Window
    {
        private readonly HttpClient _client;
        private readonly Product _product;
        private readonly DataGrid _productsGrid;

        public DeleteWindow(HttpClient client, Product product, DataGrid productsGrid)
        {
            _client = client;
            _product = product;
            _productsGrid = productsGrid;

            Title = "Confirm Delete";
            Width = 400;
            Height = 200;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            var grid = new Grid { Margin = new Thickness(20) };

            var stackPanel = new StackPanel();

            var warningText = new TextBlock
            {
                Text = $"Are you sure you want to delete the following product?\n\nProduct ID: {product.ProductID}\nName: {product.Name}\nSKU: {product.SKU}",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 20)
            };
            stackPanel.Children.Add(warningText);

            var executeButton = new Button
            {
                Content = "Execute Delete",
                Height = 35,
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(20, 5, 20, 5),
                Background = new SolidColorBrush(Colors.Red),
                Foreground = new SolidColorBrush(Colors.White),
                FontWeight = FontWeights.SemiBold
            };
            executeButton.Click += ExecuteDelete_Click;
            stackPanel.Children.Add(executeButton);

            // Cancel button
            var cancelButton = new Button
            {
                Content = "Cancel",
                Height = 35,
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(20, 5, 20, 5)
            };
            cancelButton.Click += (s, e) => Close();
            stackPanel.Children.Add(cancelButton);

            grid.Children.Add(stackPanel);
            Content = grid;
        }

        private async void ExecuteDelete_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (!int.TryParse(_product.ProductID, out int productID))
                {
                    MessageBox.Show("Invalid Product ID. Unable to delete.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var pid = new ProductId { product_id = productID };
                var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
                var data = JsonSerializer.Serialize(pid, options);
                var content = new StringContent(data, Encoding.UTF8 ,"application/json");


                var response = await _client.PostAsync("/products/delete", content);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Product deleted successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                  
                    Close();
                }
                else
                {
                    MessageBox.Show($"Failed to delete product: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }
        }

        private List<Product> MapRowsToProducts(List<List<string>> rows, List<string> columns)
        {
            var products = new List<Product>();
            foreach (var row in rows)
            {
                products.Add(new Product
                {
                    ProductID = GetValueOrDefault(row, columns, "ProductID"),
                    Name = GetValueOrDefault(row, columns, "Name"),
                    SKU = GetValueOrDefault(row, columns, "SKU"),
                    CategoryName = GetValueOrDefault(row, columns, "CategoryName"),
                    Quantity = GetValueOrDefault(row, columns, "Quantity") ?? "N/A",
                    UnitPrice = GetValueOrDefault(row, columns, "UnitPrice") ?? "N/A",
                    Barcode = GetValueOrDefault(row, columns, "Barcode") ?? "N/A",
                    CreatedAt = GetValueOrDefault(row, columns, "CreatedAt") ?? "N/A",
                    UpdatedAt = GetValueOrDefault(row, columns, "UpdatedAt") ?? "N/A"
                });
            }
            return products;
        }

        private string GetValueOrDefault(List<string> row, List<string> columns, string columnName)
        {
            int index = columns.IndexOf(columnName);
            return index >= 0 && index < row.Count ? row[index] : null;
        }
    }


    

public class Supplier
    {
        [JsonPropertyName("purchase_order_id")]
        public int PurchaseOrderID { get; set; }

        [JsonPropertyName("order_date")]
        public string OrderDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal TotalAmount { get; set; }

        [JsonPropertyName("supplier_name")]
        public string SupplierName { get; set; }

        [JsonPropertyName("contact_name")]
        public string ContactName { get; set; }

        [JsonPropertyName("phone")]
        public string Phone { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("address")]
        public string Address { get; set; }
    }

    public class SupplierResponse
    {
        public string Status { get; set; }
        public List<Supplier> Data { get; set; }
    }


    public partial class Dashboard : Window
    {
        private readonly HttpClient _client = new HttpClient();

        public Dashboard()
        {
            InitializeComponent();
            _client.BaseAddress = new Uri("http://127.0.0.1:3500");

            DashboardData _dashboardData = new DashboardData();
            DataContext = _dashboardData;
            Details_gird.Visibility = Visibility.Visible;
            Setvisibility(1);
        }

        public Dashboard(string s)
        {
            InitializeComponent();
            _client.BaseAddress = new Uri("http://127.0.0.1:3500");

            DashboardData _dashboardData = new DashboardData();
            DataContext = _dashboardData;
            Details_gird.Visibility = Visibility.Visible;
            Setvisibility(1);

            roletextbox.Text = "Role: " + s;
        }





        private async void Dashboard_Loaded(object sender, RoutedEventArgs e)
        {



        }

        private async System.Threading.Tasks.Task LoadProductsDataAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("/products");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var queryResult = JsonSerializer.Deserialize<QueryResult>(json, options);

                    if (queryResult?.Rows != null && queryResult.Columns != null)
                    {
                        var products = MapRowsToProducts(queryResult.Rows, queryResult.Columns);

                        Dispatcher.Invoke(() =>
                        {
                            Products_gird.ItemsSource = products;
                        });
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load data: {response.StatusCode}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<Product> MapRowsToProducts(List<List<string>> rows, List<string> columns)
        {
            var products = new List<Product>();

            if (rows == null || columns == null || rows.Count == 0)
                return products;

            foreach (var row in rows)
            {
                if (row == null || row.Count == 0)
                    continue;

                products.Add(new Product
                {
                    ProductID = GetValueOrDefault(row, columns, "ProductID"),
                    Name = GetValueOrDefault(row, columns, "Name"),
                    SKU = GetValueOrDefault(row, columns, "SKU"),
                    CategoryName = GetValueOrDefault(row, columns, "CategoryName"),
                    Quantity = GetValueOrDefault(row, columns, "Quantity") ?? "N/A",
                    UnitPrice = GetValueOrDefault(row, columns, "UnitPrice") ?? "N/A",
                    Barcode = GetValueOrDefault(row, columns, "Barcode") ?? "N/A",
                    CreatedAt = GetValueOrDefault(row, columns, "CreatedAt") ?? "N/A",
                    UpdatedAt = GetValueOrDefault(row, columns, "UpdatedAt") ?? "N/A"
                });
            }

            return products;
        }

        private string GetValueOrDefault(List<string> row, List<string> columns, string columnName)
        {
            int index = columns.IndexOf(columnName);
            return index >= 0 && index < row.Count ? row[index] : null;
        }

        private void Log_out_Click(object sender, RoutedEventArgs e)
        {
            MainWindow window = new MainWindow();
            window.Show();
            Close();
        }

        private void Products_gird_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {









        }

        public void Setvisibility(int num)
        {

            if (num == 1)
            {
                orderListView.Visibility = Visibility.Collapsed;
                listlogs.Visibility = Visibility.Collapsed;
                CrudgirdSupplier.Visibility = Visibility.Hidden;
                Details_gird.Visibility = Visibility.Visible;
                DataGrid productgird = (DataGrid)this.FindName("Products_gird");
                productgird.Visibility = Visibility.Hidden;
                Crudgird.Visibility = Visibility.Hidden;
                CrudgirdSupplier.Visibility = Visibility.Hidden;
                Suppliergird.Visibility = Visibility.Hidden;
            }




            else if (num == 2)
            {
                orderListView.Visibility = Visibility.Collapsed;

                listlogs.Visibility = Visibility.Collapsed;
                Details_gird.Visibility = Visibility.Hidden;
                CrudgirdSupplier.Visibility = Visibility.Hidden;
                DataGrid productgird = (DataGrid)this.FindName("Products_gird");
                productgird.Visibility = Visibility.Visible;
                Crudgird.Visibility = Visibility.Visible;
                Suppliergird.Visibility = Visibility.Hidden;
                CrudgirdSupplier.Visibility = Visibility.Hidden;

            }
            else if(num == 3)
            {
                orderListView.Visibility = Visibility.Collapsed;
                listlogs.Visibility = Visibility.Collapsed;
                Details_gird.Visibility = Visibility.Hidden;
                Products_gird.Visibility = Visibility.Hidden;
                Suppliergird.Visibility= Visibility.Visible;
                CrudgirdSupplier.Visibility = Visibility.Visible;

            }
            else if (num == 4)
            {
                orderListView.Visibility = Visibility.Visible;
                listlogs.Visibility = Visibility.Collapsed;
                CrudgirdSupplier.Visibility = Visibility.Hidden;
                Details_gird.Visibility = Visibility.Collapsed;
                DataGrid productgird = (DataGrid)this.FindName("Products_gird");
                productgird.Visibility = Visibility.Hidden;
                Crudgird.Visibility = Visibility.Hidden;
                CrudgirdSupplier.Visibility = Visibility.Hidden;
                Suppliergird.Visibility = Visibility.Hidden;




            }
            else if(num == 5)
            {

                orderListView.Visibility = Visibility.Collapsed;
                Details_gird.Visibility = Visibility.Collapsed;
                Products_gird.Visibility = Visibility.Collapsed;
                Suppliergird.Visibility = Visibility.Collapsed;
                CrudgirdSupplier.Visibility = Visibility.Collapsed;
                Crudgird.Visibility= Visibility.Collapsed;

                listlogs.Visibility = Visibility.Visible;

            }




        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }


        private void dashboardbutton_Click_1(object sender, RoutedEventArgs e)
        {



            Setvisibility(1);




        }

        private async void productsbutton_Click(object sender, RoutedEventArgs e)
        {
            Loaded += Dashboard_Loaded;
            Setvisibility(2);
            try
            {
                await LoadProductsDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Setvisibility(2);

        }

        private async void refreshbutton_click(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadProductsDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error! refresh failed...", MessageBoxButton.OK, MessageBoxImage.Error);
            }


        }

        private async void createbutton_click(object sender, RoutedEventArgs e)
        {


            var createWindow = new CreateProductWindow();
            if (createWindow.ShowDialog() == true)
            {
                await LoadProductsDataAsync();
            }



        }

        private async void updatebutton_click(object sender, RoutedEventArgs e)
        {
        
            
                List<Product> products = Products_gird.ItemsSource as List<Product>;
                var updateWindow = new updatewindow(products);
                updateWindow.ShowDialog();

        }

        private void searchbutton_click(object sender, RoutedEventArgs e)
        {

            var searchWindow = new SearchWindow(_client, Products_gird);
            searchWindow.ShowDialog();
        }

        private void deletebutton_click(object sender, RoutedEventArgs e)
        {

            var selectedProduct = Products_gird.SelectedItem as Product;
            if (selectedProduct == null)
            {
                MessageBox.Show("Please select a product to delete", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var deleteWindow = new DeleteWindow(_client, selectedProduct, Products_gird);
            deleteWindow.ShowDialog();

        }
        private void Suppliergird_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Suppliergird.SelectedItem != null)
            {
                var selectedSupplier = Suppliergird.SelectedItem as Supplier;
                if (selectedSupplier != null)
                {
                    MessageBox.Show($"Selected Supplier: {selectedSupplier.SupplierName}");
                }
            }
        }



        private async Task LoadSuppliersDataAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("/supplier");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var supplierResponse = JsonSerializer.Deserialize<SupplierResponse>(json, options);

                    if (supplierResponse?.Status == "200 OK" && supplierResponse?.Data != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Suppliergird.ItemsSource = supplierResponse.Data;
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to parse supplier data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load data: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





        private async void suppliermanagebutton_Click(object sender, RoutedEventArgs e)
        {
            Setvisibility(3);
            await LoadSuppliersDataAsync();



        }

        private async Task LoadAuditLogsAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("/logs");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var auditResponse = JsonSerializer.Deserialize<AuditResponse>(json, options);

                    if (auditResponse?.Status == "success" && auditResponse?.Data != null)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            listlogs.ItemsSource = auditResponse.Data;
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to parse audit logs data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load audit logs: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void auditlogbutton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Setvisibility(5);
                await LoadAuditLogsAsync() ;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadOrderManagementDataAsync()
        {
            try
            {
                HttpResponseMessage response = await _client.GetAsync("/orders");

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();

                    Console.WriteLine("Received JSON: " + json);

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var orderResponse = JsonSerializer.Deserialize<OrderResponse>(json, options);

                    if (orderResponse?.status == "200 OK" && orderResponse?.data != null)
                    {

                        Dispatcher.Invoke(() =>
                        {
                            orderListView.ItemsSource = orderResponse.data;
                        });
                    }
                    else
                    {
                        MessageBox.Show("Failed to parse order data.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show($"Failed to load order data: {response.StatusCode}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ordermanagebutton_Click(object sender, RoutedEventArgs e)
        {
            Setvisibility(4);

            try
            {
                await LoadOrderManagementDataAsync();  
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }






        }
    }

    public class QueryResult
    {
        public List<List<string>> Rows { get; set; }
        public List<string> Columns { get; set; }
    }
}

