using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AcManager.Tools.Objects;
using FirstFloor.ModernUI.Helpers;
using FirstFloor.ModernUI.Presentation;
using JetBrains.Annotations;

namespace AcManager.Controls {
    public interface IContextMenusProvider {
        void SetCarObjectMenu([NotNull] ContextMenu menu, [NotNull] CarObject car, [CanBeNull] CarSkinObject skin);
        void SetCarSkinObjectMenu([NotNull] ContextMenu menu, [NotNull] CarSkinObject skin);
        void SetTrackObjectMenu([NotNull] ContextMenu menu, [NotNull] TrackObjectBase track);
    }

    public class ContextMenusItems : Collection<object>{}

    public class ContextMenus {
        public static IContextMenusProvider ContextMenusProvider { get; set; }

        public static IList GetAdditionalItems(DependencyObject obj) {
            var collection = (IList)obj.GetValue(AdditionalItemsProperty);
            if (collection == null) {
                collection = new List<object>();
                obj.SetValue(AdditionalItemsProperty, collection);
            }

            return collection;
        }

        public static void SetAdditionalItems(DependencyObject obj, IList value) {
            obj.SetValue(AdditionalItemsProperty, value);
        }

        public static readonly DependencyProperty AdditionalItemsProperty = DependencyProperty.RegisterAttached("AdditionalItems", typeof(IList),
                typeof(ContextMenus), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));


        public static CarObject GetCar(DependencyObject obj) {
            return (CarObject)obj.GetValue(CarProperty);
        }

        public static void SetCar(DependencyObject obj, CarObject value) {
            obj.SetValue(CarProperty, value);
        }

        public static readonly DependencyProperty CarProperty = DependencyProperty.RegisterAttached("Car", typeof(CarObject),
                typeof(ContextMenus), new UIPropertyMetadata(OnContextMenuChanged));

        public static CarSkinObject GetCarSkin(DependencyObject obj) {
            return (CarSkinObject)obj.GetValue(CarSkinProperty);
        }

        public static void SetCarSkin(DependencyObject obj, CarSkinObject value) {
            obj.SetValue(CarSkinProperty, value);
        }

        public static readonly DependencyProperty CarSkinProperty = DependencyProperty.RegisterAttached("CarSkin", typeof(CarSkinObject),
                typeof(ContextMenus), new UIPropertyMetadata(OnContextMenuChanged));

        public static TrackObjectBase GetTrack(DependencyObject obj) {
            return (TrackObjectBase)obj.GetValue(TrackProperty);
        }

        public static void SetTrack(DependencyObject obj, TrackObjectBase value) {
            obj.SetValue(TrackProperty, value);
        }

        public static readonly DependencyProperty TrackProperty = DependencyProperty.RegisterAttached("Track", typeof(TrackObjectBase),
                typeof(ContextMenus), new UIPropertyMetadata(OnContextMenuChanged));

        private static void OnContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            if (!(d is FrameworkElement element) || e.NewValue == null) return;

            SetIsDirty(element, true);
            element.MouseEnter -= OnElementMouseEnter;
            element.MouseEnter += OnElementMouseEnter;
            element.PreviewGotKeyboardFocus -= OnElementMouseEnter;
            element.PreviewGotKeyboardFocus += OnElementMouseEnter;

            if (element.IsMouseOver) {
                UpdateContextMenu(element);
            }
        }

        private static bool GetIsDirty(DependencyObject obj) {
            return obj.GetValue(IsDirtyProperty) as bool? == true;
        }

        private static void SetIsDirty(DependencyObject obj, bool value) {
            obj.SetValue(IsDirtyProperty, value);
        }

        public static readonly DependencyProperty IsDirtyProperty = DependencyProperty.RegisterAttached("IsDirty", typeof(bool),
                typeof(ContextMenus), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.Inherits));

        private static ContextMenu CreateContextMenu([CanBeNull] FrameworkElement element, [CanBeNull] Action<ContextMenu> action) {
            var menu = new ContextMenu();

            var list = element?.GetValue(AdditionalItemsProperty) as IList;
            if (list?.Count > 0) {
                foreach (var o in list) {
                    if (o is FrameworkElement fe) {
                        fe.DataContext = element.DataContext;
                        switch (fe.Parent) {
                            case ItemsControl itemsControl:
                                itemsControl.Items.Remove(fe);
                                break;
                            case Panel panel:
                                panel.Children.Remove(fe);
                                break;
                        }
                    }

                    menu.Items.Add(o);
                }

                menu.Items.Add(new Separator());
            }

            action?.Invoke(menu);
            return menu.Items.Count != 0 ? menu : null;
        }

        [CanBeNull]
        public static ContextMenu GetCarContextMenu([CanBeNull] FrameworkElement element, [NotNull] CarObject car, [CanBeNull] CarSkinObject carSkin) {
            return CreateContextMenu(element, menu => ContextMenusProvider?.SetCarObjectMenu(menu, car, carSkin));
        }

        [CanBeNull]
        public static ContextMenu GetCarSkinContextMenu([CanBeNull] FrameworkElement element, [NotNull] CarSkinObject carSkin) {
            return CreateContextMenu(element, menu => ContextMenusProvider?.SetCarSkinObjectMenu(menu, carSkin));
        }

        [CanBeNull]
        public static ContextMenu GetTrackContextMenu([CanBeNull] FrameworkElement element, [NotNull] TrackObjectBase track) {
            return CreateContextMenu(element, menu => ContextMenusProvider?.SetTrackObjectMenu(menu, track));
        }

        private static void SetContextMenu(FrameworkElement obj, [CanBeNull] ContextMenu t, object dataContext) {
            if (t == null) {
                obj.ContextMenu = null;
                return;
            }

            t.DataContext = dataContext;
            obj.ContextMenu = t;
        }

        private static void UpdateContextMenu(FrameworkElement element) {
            if (!GetIsDirty(element)) return;
            SetIsDirty(element, false);

            var car = GetCar(element);
            if (car != null) {
                SetContextMenu(element, GetCarContextMenu(element, car, GetCarSkin(element) ?? car.SelectedSkin), car);
                return;
            }

            var skin = GetCarSkin(element);
            if (skin != null) {
                SetContextMenu(element, GetCarSkinContextMenu(element, skin), skin);
                return;
            }

            var track = GetTrack(element);
            if (track != null) {
                SetContextMenu(element, GetTrackContextMenu(element, track), track);
                return;
            }
        }

        private static void OnElementMouseEnter(object sender, EventArgs mouseEventArgs) {
            UpdateContextMenu((FrameworkElement)sender);
        }
    }
}