using System;
using System.Linq;
using Android.Content;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Android.Widget;
using CarouselView.FormsPlugin.Abstractions;
using Java.Lang;
using Xamarin.Forms;
using View = Android.Views.View;

namespace CarouselView.FormsPlugin.Android
{
    public interface ObjectAtPositionInterface
    {

        /**
        * Returns the Object for the provided position, null if position doesn't match an object (i.e. out of bounds)
        **/
        Java.Lang.Object getObjectAtPosition(int position);
    }


    public class HorizontalViewPager : ViewPager, IViewPager
	{
        private bool isSwipeEnabled = true;
	    private bool isContentWrapEnabled = true;
        private CarouselViewControl Element;
	    private static string TAG = "HorizontalViewPager";
	    public int height = 0;
	    private int decorHeight = 0;
	    private int widthMeasuredSpec;

	    private bool animateHeight;
	    private int rightHeight;
	    private int leftHeight;
	    private int scrollingPosition = -1;

        // Fix for #171 System.MissingMethodException: No constructor found
        public HorizontalViewPager(IntPtr intPtr, JniHandleOwnership jni) : base(intPtr, jni)
        {
            Init();
        }

        public HorizontalViewPager(Context context) : base(context, null)
        {
            Init();
        }

        public HorizontalViewPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            Init();
        }

        private void Init()
        {
            AddOnPageChangeListener(new OnPageChangeListener(i => height = i));
        }

	    private class OnPageChangeListener : ViewPager.SimpleOnPageChangeListener
	    {
	        public ScrollState state;
	        private Action<int> setHeight;

            public OnPageChangeListener(Action<int> setHeight)
            {
                this.setHeight = setHeight;
            }

	        public override void OnPageScrolled(int position, float offset, int positionOffsetPixels) { }

	        public override void OnPageSelected(int position)
	        {
	            if (state == ScrollState.Idle)
	            {
                    // measure the selected page in-case it's a change without scrolling
                    setHeight?.Invoke(0);

                    Log.Debug(TAG, "onPageSelected:" + position);
	            }
	        }

	        public override void OnPageScrollStateChanged(int state)
	        {
	            this.state = (ScrollState)state;
	        }
        }

	    public override PagerAdapter Adapter
	    {
	        set
	        {
	            if (!(value is ObjectAtPositionInterface))
	            {
	                throw new IllegalArgumentException(
	                    "WrapContentViewPage requires that PagerAdapter will implement ObjectAtPositionInterface");
	            }

	            height = 0; // so we measure the new content in onMeasure
	            base.Adapter = value;
	        }
	    }


	    /**
         * Allows to redraw the view size to wrap the content of the bigger child.
         *
         * @param widthMeasureSpec  with measured
         * @param heightMeasureSpec height measured
         */

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(widthMeasureSpec, heightMeasureSpec);

            if (isContentWrapEnabled)
            {
                widthMeasuredSpec = widthMeasureSpec;
                var mode = MeasureSpec.GetMode(heightMeasureSpec);

                if (mode == MeasureSpecMode.Unspecified || mode == MeasureSpecMode.AtMost)
                {
                    if (height == 0)
                    {
                        // measure vertical decor (i.e. PagerTitleStrip) based on ViewPager implementation
                        decorHeight = 0;
                        for (int i = 0; i < ChildCount; i++)
                        {
                            View childView = GetChildAt(i);
                            LayoutParams lp = (LayoutParams)childView.LayoutParameters;
                            if (lp != null && lp.IsDecor)
                            {
                                int vgrav = lp.Gravity & (int)GravityFlags.VerticalGravityMask;
                                bool consumeVertical = vgrav == (int)GravityFlags.Top || vgrav == (int)GravityFlags.Bottom;
                                if (consumeVertical)
                                {
                                    decorHeight += childView.MeasuredHeight;
                                }
                            }
                        }

                        // make sure that we have an height (not sure if this is necessary because it seems that onPageScrolled is called right after
                        int position = CurrentItem;
                        View child = getViewAtPosition(position);
                        if (child != null)
                        {
                            height = measureViewHeight(child);
                        }

                        Log.Debug(TAG, "onMeasure height:" + height + " decor:" + decorHeight);

                    }

                    int totalHeight = height + decorHeight + PaddingBottom + PaddingTop;
                    heightMeasureSpec = MeasureSpec.MakeMeasureSpec(totalHeight, MeasureSpecMode.Exactly);
                    Log.Debug(TAG, "onMeasure total height:" + totalHeight);
                }

                base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
            }
        }

        protected override void OnPageScrolled(int position, float offset, int positionOffsetPixels)
        {
            base.OnPageScrolled(position, offset, positionOffsetPixels);
            // cache scrolled view heights
            if (scrollingPosition != position)
            {
                scrollingPosition = position;
                // scrolled position is always the left scrolled page
                View leftView = getViewAtPosition(position);
                View rightView = getViewAtPosition(position + 1);
                if (leftView != null && rightView != null)
                {
                    leftHeight = measureViewHeight(leftView);
                    rightHeight = measureViewHeight(rightView);
                    animateHeight = true;
                    Log.Debug(TAG, "onPageScrolled heights left:" + leftHeight + " right:" + rightHeight);
                }
                else
                {
                    animateHeight = false;
                }
            }
            if (animateHeight)
            {
                int newHeight = (int)(leftHeight * (1 - offset) + rightHeight * (offset));
                if (height != newHeight)
                {
                    Log.Debug(TAG, "onPageScrolled height change:" + newHeight);
                    height = newHeight;
                    RequestLayout();
                    Invalidate();
                }
            }
        }

        private int measureViewHeight(View view)
        {
            view.Measure(GetChildMeasureSpec(widthMeasuredSpec, PaddingLeft + PaddingRight, view.LayoutParameters.Width), MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));
            return view.MeasuredHeight;
        }

        protected View getViewAtPosition(int position)
        {
            if (Adapter != null)
            {
                var objectAtPosition = ((ObjectAtPositionInterface)Adapter).getObjectAtPosition(position);
                if (objectAtPosition != null)
                {
                    for (int i = 0; i < ChildCount; i++)
                    {
                        View child = GetChildAt(i);
                        if (child != null && Adapter.IsViewFromObject(child, objectAtPosition))
                        {
                            return child;
                        }
                    }
                }
            }
            return null;
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (ev.Action == MotionEventActions.Up)
            {
                if (Element?.GestureRecognizers.GetCount() > 0)
                {
                    var gesture = Element.GestureRecognizers.First() as TapGestureRecognizer;
                    if (gesture != null)
                        gesture.Command?.Execute(gesture.CommandParameter);
                }
            }

            if (this.isSwipeEnabled)
            {
                return base.OnInterceptTouchEvent(ev);
            }

            return false;
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (this.isSwipeEnabled)
            {
                return base.OnTouchEvent(e);
            }

            return false;
        }
     //   protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
	    //{
	    //    if (isContentWrapEnabled)
	    //    {
	    //        // Forces the ViewPager to act like layout_height="wrap_content" for the current page's content height
	    //        int height = 0;
	    //        if (ChildCount > CurrentItem)
	    //        {
	    //            View child = GetChildAt(CurrentItem);
	    //            child.Measure(widthMeasureSpec, MeasureSpec.MakeMeasureSpec(0, MeasureSpecMode.Unspecified));

	    //            int h = child.MeasuredHeight;
	    //            if (h > height) height = h;
	    //        }

	    //        heightMeasureSpec = MeasureSpec.MakeMeasureSpec(height, MeasureSpecMode.Exactly);

	    //    }

	    //    base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
	    //}

        public void SetPagingEnabled(bool enabled)
        {
            this.isSwipeEnabled = enabled;
        }

	    public void SetContentWrapEnabled(bool enabled)
	    {
	        this.isContentWrapEnabled = enabled;
	    }

        public void SetElement(CarouselViewControl element)
        {
            this.Element = element;
        }
	}
}
