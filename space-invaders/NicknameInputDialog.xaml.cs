using System.Windows;

namespace Space_intruders
{
    public partial class NicknameInputDialog : Window
    {
        public string Nickname { get; private set; } = "Player"; // Default value

        public NicknameInputDialog()
        {
            InitializeComponent();
            NicknameTextBox.Focus(); 
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            string enteredName = NicknameTextBox.Text.Trim();
            if (!string.IsNullOrWhiteSpace(enteredName))
            {
                // Basic validation: remove commas to prevent CSV issues
                Nickname = enteredName.Replace(",", "");
                if (string.IsNullOrWhiteSpace(Nickname))
                {
                    Nickname = "Player";
                }
                this.DialogResult = true; 
                this.Close();
            }
            else
            {
                MessageBox.Show("Please enter a nickname.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                
                this.DialogResult = true; 
                this.Close();
            }
        }
    }
}