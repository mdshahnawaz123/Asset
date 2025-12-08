using Asset.Services;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Asset.UI
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        private readonly AuthService _auth;

        public string EnteredUsername { get; private set; }

        public LoginWindow(AuthService authService)
        {
            InitializeComponent();
            _auth = authService;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void SignIn_Click(object sender, RoutedEventArgs e)
        {
            var username = TB_Username.Text?.Trim();
            var password = PB_Password.Password;

            if (_auth.ValidateCredentials(username, password, out var matchedUser, out var error))
            {
                _auth.CurrentUser = matchedUser;
                EnteredUsername = matchedUser.Username;
                DialogResult = true;
                Close();
            }
            else
            {
                TB_Error.Text = error;
            }
        }
    }
}
