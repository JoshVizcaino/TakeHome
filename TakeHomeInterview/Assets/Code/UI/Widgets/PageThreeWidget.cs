using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;

public class PageThreeWidget : MonoBehaviour
{
    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------

    //The only thing exposed to the editor really
    [Header("Details")]
    public RawImage widgetImage;

    //Item ID
    int mID;

    // These public variables will be initialized
    // in ListPositionCtrl.InitializeBoxDependency().
    [HideInInspector] public int mWidgetID; // The same as the order in `mWidgets`
    [HideInInspector] public PageThreeWidget mLastWidget;
    [HideInInspector] public PageThreeWidget mNextWidget;
    private int mContentID;

    private ScrollPositionCtrl mPositionCtrl;
    private BaseScrollBank mScrollBank;
    private CurveResolver mPositionCurve;
    private CurveResolver mScaleCurve;

    public Action<float> UpdatePosition { private set; get; }

    // Position variables
    // Position calculated here is in the local space of the list
    private float mUnitPosition; // The distance between boxes
    private float mLowerBoundPosition; // The left/down-most position of the box
    private float mUpperBoundPosition; // The right/up-most position of the box
    private float mChangeSideLowerBoundPosition; // mChangeSide(Lower/Upper)BoundPosition is the boundary for checking that
    private float mChangeSideUpperBoundPosition; // whether to move the box to the other end or not

    //------------------------------------------------------------------------------------
    // Functions
    //------------------------------------------------------------------------------------

    public void SetImage(Texture inTexture)
    {
        widgetImage.texture = inTexture;
    }

    public int ID
    {
        get => mID;
        set => value = mID;
    }
    

    // Output the information of the box to the Debug.Log
    public void ShowBoxInfo()
    {
        Debug.Log("Box ID: " + mWidgetID.ToString() +
                  ", Content ID: " + mContentID.ToString() +
                  ", Content: " + mScrollBank.GetScrollContent(mContentID));
    }

    // Get the content ID of the box
    public int GetContentID()
    {
        return mContentID;
    }

    // Initialize the box.
    public void Initialize(ScrollPositionCtrl scrollPositionCtrl)
    {
        mPositionCtrl = scrollPositionCtrl;
        mScrollBank = mPositionCtrl.scrollBank;

        switch (mPositionCtrl.direction)
        {
            case ScrollPositionCtrl.Direction.Vertical:
                UpdatePosition = MoveVertically;
                break;
            case ScrollPositionCtrl.Direction.Horizontal:
                UpdatePosition = MoveHorizontally;
                break;
        }

        mUnitPosition = mPositionCtrl.unitPos;
        mLowerBoundPosition = mPositionCtrl.lowerBoundPos;
        mUpperBoundPosition = mPositionCtrl.upperBoundPos;
        mChangeSideLowerBoundPosition = mLowerBoundPosition + mUnitPosition * 0.5f;
        mChangeSideUpperBoundPosition = mUpperBoundPosition - mUnitPosition * 0.5f;

        mPositionCurve = new CurveResolver(
            mPositionCtrl.widgetPositionCurve,
            mChangeSideLowerBoundPosition, mChangeSideUpperBoundPosition);
        mScaleCurve = new CurveResolver(
            mPositionCtrl.widgetScaleCurve,
            mChangeSideLowerBoundPosition, mChangeSideUpperBoundPosition);

        InitialPosition();
        InitialContent();
    }

    // Initialize the local position of the widget according to its ID
    private void InitialPosition()
    {
        int num_of_widgets = mPositionCtrl.mWidgets.Length;
        float major_position = mUnitPosition * (mWidgetID * -1 + num_of_widgets / 2);
        float passive_position;

        // If there are even number of widgets, adjust the position one half mUnitPosition down.
        if ((num_of_widgets & 0x1) == 0)
        {
            major_position = mUnitPosition * (mWidgetID * -1 + num_of_widgets / 2) - mUnitPosition / 2;
        }

        passive_position = Getpassive_position(major_position);

        switch (mPositionCtrl.direction)
        {
            case ScrollPositionCtrl.Direction.Vertical:
                transform.localPosition = new Vector3(
                    passive_position, major_position, transform.localPosition.z);
                break;
            case ScrollPositionCtrl.Direction.Horizontal:
                transform.localPosition = new Vector3(
                    major_position, passive_position, transform.localPosition.z);
                break;
        }

        UpdateScale(major_position);
    }

    /* Move the box vertically and adjust its final position and size.
	 *
	 * This function is the UpdatePosition in the vertical mode.
	 *
	 * @param delta The moving distance
	 */
    private void MoveVertically(float delta)
    {
        bool need_to_update_to_last_content = false;
        bool need_to_update_to_next_content = false;
        float major_position = Getmajor_position(transform.localPosition.y + delta,
            ref need_to_update_to_last_content, ref need_to_update_to_next_content);
        float passive_position = Getpassive_position(major_position);

        transform.localPosition = new Vector3(
            passive_position, major_position, transform.localPosition.z);
        UpdateScale(major_position);

        if (need_to_update_to_last_content)
            UpdateToLastContent();
        else if (need_to_update_to_next_content)
            UpdateToNextContent();
    }

    /* Move the box horizontally and adjust its final position and size.
	 *
	 * This function is the UpdatePosition in the horizontal mode.
	 *
	 * @param delta The moving distance
	 */
    private void MoveHorizontally(float delta)
    {
        bool need_to_update_to_last_content = false;
        bool need_to_update_to_next_content = false;
        float major_position = Getmajor_position(transform.localPosition.x + delta,
            ref need_to_update_to_last_content, ref need_to_update_to_next_content);
        float passive_position = Getpassive_position(major_position);

        transform.localPosition = new Vector3(
            major_position, passive_position, transform.localPosition.z);
        UpdateScale(major_position);

        if (need_to_update_to_last_content)
            UpdateToLastContent();
        else if (need_to_update_to_next_content)
            UpdateToNextContent();
    }

    /* Get the major position according to the requested position
	 * If the box exceeds the boundary, one of the passed flags will be set
	 * to indicate that the content needs to be updated.
	 *
	 * @param positionValue The requested position
	 * @param need_to_update_to_last_content Is it need to update to the last content?
	 * @param need_to_update_to_next_content Is it need to update to the next content?
	 * @return The decided major position
	 */
    private float Getmajor_position(float positionValue,
        ref bool need_to_update_to_last_content, ref bool need_to_update_to_next_content)
    {
        float beyondPos = 0.0f;
        float majorPos = positionValue;

        if (positionValue < mChangeSideLowerBoundPosition)
        {
            beyondPos = positionValue - mLowerBoundPosition;
            majorPos = mUpperBoundPosition - mUnitPosition + beyondPos;
            need_to_update_to_last_content = true;
        }
        else if (positionValue > mChangeSideUpperBoundPosition)
        {
            beyondPos = positionValue - mUpperBoundPosition;
            majorPos = mLowerBoundPosition + mUnitPosition + beyondPos;
            need_to_update_to_next_content = true;
        }

        return majorPos;
    }

    // Get the passive position according to the major position
    private float Getpassive_position(float major_position)
    {
        float passivePosFactor = mPositionCurve.Evaluate(major_position);
        return mUpperBoundPosition * passivePosFactor;
    }

    // Scale the listBox according to the major position
    private void UpdateScale(float major_position)
    {
        float scaleValue = mScaleCurve.Evaluate(major_position);
        transform.localScale = new Vector3(scaleValue, scaleValue, transform.localScale.z);
    }

    // Initialize the content of the widget.
    private void InitialContent()
    {
        // Get the content ID of the centered widget
        mContentID = mPositionCtrl.centeredContentID;

        // Adjust the contentID according to its initial order.
        mContentID += mWidgetID - mPositionCtrl.mWidgets.Length / 2;

        // In the linear mode, disable widget if needed
        if (mPositionCtrl.scrollType == ScrollPositionCtrl.ScrollType.Linear)
        {
            // Disable the widgets at the upper half of the list
            // which will hold the item at the tail of the contents.
            if (mContentID < 0)
            {
                mPositionCtrl.numOfUpperDisabledBoxes += 1;
                gameObject.SetActive(false);
            }
            // Disable the box at the lower half of the list
            // which will hold the repeated item.
            else if (mContentID >= mScrollBank.GetScrollLength())
            {
                mPositionCtrl.numOfLowerDisabledBoxes += 1;
                gameObject.SetActive(false);
            }
        }

        // Round the content id
        while (mContentID < 0)
            mContentID += mScrollBank.GetScrollLength();
        mContentID = mContentID % mScrollBank.GetScrollLength();

    }

    // Update the content to the last content of the next widget
    private void UpdateToLastContent()
    {
        mContentID = mNextWidget.GetContentID() - 1;
        mContentID = (mContentID < 0) ? mScrollBank.GetScrollLength() - 1 : mContentID;

        if (mPositionCtrl.scrollType == ScrollPositionCtrl.ScrollType.Linear)
        {
            if (mContentID == mScrollBank.GetScrollLength() - 1 ||
                !mNextWidget.isActiveAndEnabled)
            {
                // If the widget has been disabled at the other side,
                // decrease the counter of the other side.
                if (!isActiveAndEnabled)
                    --mPositionCtrl.numOfLowerDisabledBoxes;

                // In linear mode, don't display the content of the other end
                gameObject.SetActive(false);
                ++mPositionCtrl.numOfUpperDisabledBoxes;
            }
            else if (!isActiveAndEnabled)
            {
                // The disabled widgets from the other end will be enabled again,
                // if the next widget is enabled.
                gameObject.SetActive(true);
                --mPositionCtrl.numOfLowerDisabledBoxes;
            }
        }

    }

    // Update the content to the next content of the last ListBox
    private void UpdateToNextContent()
    {
        mContentID = mLastWidget.GetContentID() + 1;
        mContentID = (mContentID == mScrollBank.GetScrollLength()) ? 0 : mContentID;

        if (mPositionCtrl.scrollType == ScrollPositionCtrl.ScrollType.Linear)
        {
            if (mContentID == 0 || !mLastWidget.isActiveAndEnabled)
            {
                if (!isActiveAndEnabled)
                    --mPositionCtrl.numOfUpperDisabledBoxes;

                // In linear mode, don't display the content of the other end
                gameObject.SetActive(false);
                ++mPositionCtrl.numOfLowerDisabledBoxes;
            }
            else if (!isActiveAndEnabled)
            {
                gameObject.SetActive(true);
                --mPositionCtrl.numOfUpperDisabledBoxes;
            }
        }

    }

    // The class for converting the custom range to fit the AnimationCurve for evaluating the final value.
    private class CurveResolver
    {
        private AnimationCurve _curve;
        private float _maxValue;
        private float _minValue;

        /* Constructor
		 *
		 * @param curve The target AnimationCurve to fit
		 * @param minValue The custom minimum value
		 * @param maxValue The custom maximum value
		 */
        public CurveResolver(AnimationCurve curve, float minValue, float maxValue)
        {
            _curve = curve;
            _minValue = minValue;
            _maxValue = maxValue;
        }

        /* Convert the input value to the value of interpolation between [minValue, maxValue]
		 * and pass the result to the curve to get the final value.
		 */
        public float Evaluate(float value)
        {
            float lerpValue = Mathf.InverseLerp(_minValue, _maxValue, value);
            return _curve.Evaluate(lerpValue);
        }
    }
}
