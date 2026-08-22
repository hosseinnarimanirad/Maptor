//using Microsoft.Xaml.Behaviors;
//using System;
//using System.Windows.Controls; 

//namespace IRI.Maptor.Presentation.Wpf.Assets.Behaviors
//{
//    public class ListboxAutoScrollToSelectedItemBehavior : Behavior<ListBox>
//    {
//        /// <summary>
//        ///  When Behavior is attached
//        /// </summary>
//        protected override void OnAttached()
//        {
//            base.OnAttached();
//            this.AssociatedObject.SelectionChanged += AssociatedObject_SelectionChanged;
//        }

//        /// <summary>
//        /// On Selection Changed
//        /// </summary>
//        /// <param name="sender"></param>
//        /// <param name="e"></param>
//        void AssociatedObject_SelectionChanged(object sender,
//                                               SelectionChangedEventArgs e)
//        {
//            if (sender is ListBox)
//            {
//                ListBox listBox = (sender as ListBox);
//                if (listBox.SelectedItem != null)
//                {
//                    listBox.Dispatcher.BeginInvoke(
//                        (Action)(() =>
//                        {
//                            listBox.UpdateLayout();
//                            if (listBox.SelectedItem !=
//                            null)
//                                listBox.ScrollIntoView(
//                                listBox.SelectedItem);
//                        }));
//                }
//            }
//        }
//        /// <summary>
//        /// When behavior is detached
//        /// </summary>
//        protected override void OnDetaching()
//        {
//            base.OnDetaching();
//            this.AssociatedObject.SelectionChanged -= AssociatedObject_SelectionChanged;

//        }
//    }
//}