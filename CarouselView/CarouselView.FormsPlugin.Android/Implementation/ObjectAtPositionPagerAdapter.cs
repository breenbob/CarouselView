using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;

namespace CarouselView.FormsPlugin.Android.Implementation
{
    public abstract class ObjectAtPositionPagerAdapter : PagerAdapter, ObjectAtPositionInterface
    {
        protected List<Object> objects = new List<>();

        public override Object instantiateItem(ViewGroup container, int position)
        {
            Object obj = instantiateItemObject(container, position);
            objects.put(position, obj);
            return obj;
        }

        /**
         * Replaces @see PagerAdapter#instantiateItem and handles objects tracking for getObjectAtPosition
         */
        public abstract Object instantiateItemObject(ViewGroup container, int position);

        public void destroyItem(ViewGroup container, int position, Object obj)
        {
            objects.remove(position);
            destroyItemObject(container, position, obj);
        }

        /**
         * Replaces @see PagerAdapter#destroyItem and handles objects tracking for getObjectAtPosition
         */
        public abstract void destroyItemObject(ViewGroup container, int position, Object obj);

        public override Object getObjectAtPosition(int position)
        {
            return objects.get(position);
        }
    }
}