using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfMultiselectTreeViewKit.UserControl
{
    public class EditTextBox : TextBox
    {
        public bool EditingTrigger
        {
            get { return (bool)GetValue(EditingTriggerProperty); }
            set { SetValue(EditingTriggerProperty, value); }
        }

        // Using a DependencyProperty as the backing store for EditingTrigger.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EditingTriggerProperty =
            DependencyProperty.Register("EditingTrigger", typeof(bool), 
            typeof(EditTextBox),
            new FrameworkPropertyMetadata(false) { BindsTwoWayByDefault = true });

        
        #region Private fields

        private string mStartText;

        #endregion Private fields

        #region Constructor

        static EditTextBox()
        {
            //DefaultStyleKeyProperty.OverrideMetadata(typeof(EditTextBox), new FrameworkPropertyMetadata(typeof(EditTextBox)));
        }

        public EditTextBox()
        {
            //Loaded += OnTreeViewEditTextBoxLoaded;
            IsVisibleChanged += OnIsVisibleChanged;
        }
        
        void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                Dispatcher.BeginInvoke((Action)delegate
                {
                    Keyboard.Focus(this);
                }, DispatcherPriority.Render);
            }
        }

        #endregion Constructor

        #region Methods

        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            mStartText = Text;
            string fileName = null;
            try
            {
                fileName = Path.GetFileNameWithoutExtension(Text);
            }
            catch
            {
                //supressing error
            }
            if(fileName==null)
            {
                SelectAll();
                return;
            }
            Select(0, fileName.Length);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            EditingTrigger = false;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (!e.Handled)
            {
                Key key = e.Key;
                switch (key)
                {
                    case Key.Escape:
                        Text = mStartText;
                        EditingTrigger = false;
                        break;
                }
            }
        }

        private void OnTreeViewEditTextBoxLoaded(object sender, RoutedEventArgs e)
        {
            BindingExpression be = GetBindingExpression(TextProperty);
            if (be != null) be.UpdateTarget();
            FocusHelper.Focus(this);
        }

        #endregion
    }

    public static class FocusHelper
    {
        #region Public methods

        public static void Focus(EditTextBox element)
        {
            //System.Diagnostics.Debug.WriteLine("Focus textbox with helper:" + element.Text);
            FocusCore(element);
            element.BringIntoView();
        }

        public static void Focus(TreeViewItem element, bool bringIntoView = false)
        {
            //System.Diagnostics.Debug.WriteLine("FocusHelper focusing " + (bringIntoView ? "[into view] " : "") + element.DataContext);
            FocusCore(element);

            if (bringIntoView)
            {
                FrameworkElement itemContent = (FrameworkElement)element.Template.FindName("headerBorder", element);
                if (itemContent != null)   // May not be rendered yet...
                {
                    itemContent.BringIntoView();
                }
            }
        }

        public static void Focus(TreeView element)
        {
            //System.Diagnostics.Debug.WriteLine("Focus Tree with helper");
            FocusCore(element);
            element.BringIntoView();
        }

        private static void FocusCore(FrameworkElement element)
        {
            //System.Diagnostics.Debug.WriteLine("Focusing element " + element.ToString());
            //System.Diagnostics.Debug.WriteLine(Environment.StackTrace);
            if (!element.Focus())
            {
                //System.Diagnostics.Debug.WriteLine("- Element could not be focused, invoking in dispatcher thread");
                element.Dispatcher.BeginInvoke(DispatcherPriority.Input, new ThreadStart(() => element.Focus()));
            }

#if DEBUG
            // no good idea, seems to block sometimes
            int i = 0;
            while (i < 5)
            {
                if (element.IsFocused)
                {
                    //if (i > 0)
                    //    System.Diagnostics.Debug.WriteLine("- Element is focused now in round " + i + ", leaving");
                    return;
                }
                Thread.Sleep(20);
                i++;
            }
            //if (i >= 5)
            //{
            //    System.Diagnostics.Debug.WriteLine("- Element is not focused after 500 ms, giving up");
            //}
#endif
        }

        #endregion Public methods
    }
}
