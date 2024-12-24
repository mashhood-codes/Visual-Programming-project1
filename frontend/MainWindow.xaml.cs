using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Http;
using System.Text.Json;
namespace frontend
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 


     struct credentials
    {
        public string? username { get; set; }
        public string? password { get; set; }
    }
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void PasswordBox_TextInput(object sender, TextCompositionEventArgs e)
        {

        }

        private void event_login(object sender, RoutedEventArgs e)
        {
            TextBox iusername = this.FindName("iusr") as TextBox;

            string usr = iusername.Text;
            if (iusername == null)
            {

            }

            PasswordBox inputpasswd = this.FindName("ipasswd") as PasswordBox;
            string passwd = inputpasswd.Password;
            if (inputpasswd == null)
            {



            }
            var userobject = new credentials
            {
                username = usr,
                password = passwd
            };
            var jsonobject = JsonSerializer.Serialize(userobject);
            var data = new StringContent(jsonobject, Encoding.UTF8, "application/json");
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://127.0.0.1:3500");
                var res = client.PostAsync("/login", data).Result;
                var val = ((int)res.StatusCode);
                if (val == 200)
                {
                    MessageBox.Show("Login Successful! ...");
                    Dashboard dashboard = new Dashboard(usr);
                    dashboard.Show();
                    this.Close();

                }
                else
                {
                    MessageBox.Show("Invalid Username or Password! ...");


                }


            }


        }
    }
}