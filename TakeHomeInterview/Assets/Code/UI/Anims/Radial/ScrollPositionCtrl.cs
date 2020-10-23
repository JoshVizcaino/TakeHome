// Handle the controlling event and send the moving information to the widgets

using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public interface IControlEventHandler:
	IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
{}

// The callback for passing the onClick event sent from the clicked widget.
// The int parameter will be the ID of the content which the clicked widget holds.
[System.Serializable]
public class WidgetClickEvent : UnityEvent<int>
{}

// The callback for the event of the scroll rect.
//The ScrollPositionCtrl parameter is the scrollrect which fires the event.
[System.Serializable]
public class ScrollEvent : UnityEvent<ScrollPositionCtrl>
{}

public class ScrollPositionCtrl : MonoBehaviour, IControlEventHandler
{
	public enum ScrollType
	{
		Circular,
		Linear
	};

	public enum ControlMode
	{
		Drag,       // By the mouse pointer or finger
		Function,   // By the calling MoveOneUnitUp/MoveOneUnitDown function
		MouseWheel  // By the mouse wheel
	};

	public enum Direction
	{
		Vertical,
		Horizontal
	};

	public enum PositionState
	{
		Top,    // The items reach the top
        Middle,	// The items don't reach either end
		Bottom	// The items reach the bottom
	};

    //------------------------------------------------------------------------------------
    // Settings
    //------------------------------------------------------------------------------------
    [Tooltip("The type of scroll.")]
	public ScrollType scrollType = ScrollType.Circular;
	[Tooltip("The controlling mode of the scroll.")]
	public ControlMode controlMode = ControlMode.Drag;
	[Tooltip("Should a widget align in the middle of the scroll after sliding?")]
	public bool alignMiddle = false;
	[Tooltip("The major moving direction of the scroll.")]
	public Direction direction = Direction.Vertical;

    //------------------------------------------------------------------------------------
    // Containers
    //------------------------------------------------------------------------------------
    [Tooltip("The game object which holds the content bank for the scroll." +
	         "It will be the derived class of the BaseScrollBank.")]
	public ScrollBank scrollBank;
	[Tooltip("Specify the initial content ID for the centered box.")]
	public int centeredContentID = 0;
	[Tooltip("The boxes which belong to this scroll.")]
	public PageThreeWidget[] mWidgets;

    //------------------------------------------------------------------------------------
    // Appearance
    //------------------------------------------------------------------------------------
    [Tooltip("The distance between each widget. The larger, the closer.")]
	public float widgetDensity = 2.0f;
	[Tooltip("The curve specifying the widget position. " +
	         "The x axis is the major position of the widget, which is mapped to [0, 1]. " +
	         "The y axis defines the factor of the passive position of the box. " +
	         "Point (0.5, 0) is the center of the scroll layout.")]
	public AnimationCurve widgetPositionCurve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);
    [Tooltip("The curve specifying the widget scale. " +
             "The x axis is the major position of the widget, which is mapped to [0, 1]. " +
             "The y axis specifies the value of 'localScale' of the widget at the " +
             "corresponding position.")]
    public AnimationCurve widgetScaleCurve = AnimationCurve.Constant(0.0f, 1.0f, 1.0f);
    [Tooltip("The curve specifying the movement of the widget. " +
             "The x axis is the moving duration in seconds, which starts from 0. " +
             "The y axis is the factor of the releasing velocity in Drag mode, or " +
             "the factor of the target position in Function and Mouse Wheel modes.")]
    public AnimationCurve widgetMovementCurve = new AnimationCurve(
		new Keyframe(0.0f, 1.0f, 0.0f, -2.5f),
		new Keyframe(1.0f, 0.0f, 0.0f, 0.0f));

    //------------------------------------------------------------------------------------
    // Events
    //------------------------------------------------------------------------------------
    [Tooltip("The callbacks for the event of the clicking on widgets." +
	         "The registered callbacks will be added to the 'onClick' event of widgets, " +
	         "therefore, widgets can be 'Button's should you want to click on them")]
	public WidgetClickEvent onWidgetClick;
	// The callback will be invoked when the scroll is moving.
	public ScrollEvent onScrollMove;

	// The canvas plane which the scrolling list is at.
	private Canvas mParentCanvas;

	// The constrains of position in the local space of the canvas plane.
	private float mCanvasMaxPosition;
	public float unitPos { get; private set; }
	public float lowerBoundPos { get; private set; }
	public float upperBoundPos { get; private set; }

	// Delegate functions
	private Action<PointerEventData, TouchPhase> mInputPositionHandler;
	private Action<Vector2> mScrollHandler;

	// Variables for moving widgets
	private IMovementCtrl mMovementCtrl;
	// Input mouse/finger position in the local space of the scroll.
	private float mDeltaInputPosition;
	private float mDeltaDistanceToCenter = 0.0f;

	// Variables for linear mode
	private PositionState mPositionState = PositionState.Middle;
	[HideInInspector]
	public int numOfUpperDisabledBoxes = 0;
	[HideInInspector]
	public int numOfLowerDisabledBoxes = 0;
	private int mMaxNumOfDisabledWidgets = 0;

	//A widget will initialize its variables from here, so ScrollPositionCtrl must be executed before PageThreeWidget. You have to set the execution order in the inspector.
	private void Start()
	{
		Application.targetFrameRate = 60;
		InitializePositionVars();
		InitializeInputFunction();
		InitializeBoxDependency();
		mMaxNumOfDisabledWidgets = mWidgets.Length / 2;
		foreach (PageThreeWidget widget in mWidgets)
            widget.Initialize(this);
	}

	private void InitializePositionVars()
	{
		// The the reference of canvas plane 
		mParentCanvas = GetComponentInParent<Canvas>();

		//Get the max position of canvas plane in the canvas space.
		//Assume that the origin of the canvas space is at the center of canvas plane.
		RectTransform rectTransform = mParentCanvas.GetComponent<RectTransform>();

		switch (direction) {
			case Direction.Vertical:
				mCanvasMaxPosition = rectTransform.rect.height / 2;
				break;
			case Direction.Horizontal:
				mCanvasMaxPosition = rectTransform.rect.width / 2;
				break;
		}

		unitPos = mCanvasMaxPosition / widgetDensity;
		lowerBoundPos = unitPos * (-1 * mWidgets.Length / 2 - 1);
		upperBoundPos = unitPos * (mWidgets.Length / 2 + 1);

		// If there are even number of widgets, narrow the boundary for 1 mUnitPosition.
		if ((mWidgets.Length & 0x1) == 0) {
			lowerBoundPos += unitPos / 2;
			upperBoundPos -= unitPos / 2;
		}
	}

	private void InitializeBoxDependency()
	{
		// Set the widget ID according to the order in the container `mWidgets`
		for (int i = 0; i < mWidgets.Length; ++i)
            mWidgets[i].mWidgetID = i;

		// Set the neighbor widgets
		for (int i = 0; i < mWidgets.Length; ++i) {
            mWidgets[i].mLastWidget = mWidgets[(i - 1 >= 0) ? i - 1 : mWidgets.Length - 1];
            mWidgets[i].mNextWidget = mWidgets[(i + 1 < mWidgets.Length) ? i + 1 : 0];
		}
	}

    // Initialize the corresponding handlers for the selected controlling mode. 
    //The unused handler will be assigned a dummy function to prevent the handling of the event.
	private void InitializeInputFunction()
	{
		Func<float> getAligningDistance = () => mDeltaDistanceToCenter;
		Func<PositionState> getPositionState = () => mPositionState;
		var overGoingThreshold = unitPos * 0.3f;

		switch (controlMode) {
			case ControlMode.Drag:
				mMovementCtrl = new FreeMovementCtrl(
					widgetMovementCurve, alignMiddle, overGoingThreshold,
					getAligningDistance, getPositionState);
				mInputPositionHandler = DragPositionHandler;
				mScrollHandler = (Vector2 v) => { };
				break;

			case ControlMode.Function:
				mMovementCtrl = new UnitMovementCtrl(
                    widgetMovementCurve, overGoingThreshold,
					getAligningDistance, getPositionState);
				mInputPositionHandler =
					(PointerEventData pointer, TouchPhase phase) => { };
				mScrollHandler = (Vector2 v) => { };
				break;

			case ControlMode.MouseWheel:
				mMovementCtrl = new UnitMovementCtrl(
                    widgetMovementCurve, overGoingThreshold,
					getAligningDistance, getPositionState);
				mInputPositionHandler =
					(PointerEventData pointer, TouchPhase phase) => { };
				mScrollHandler = ScrollDeltaHandler;
				break;
		}
	}

	// Callback functions for the unity event system
	public void OnBeginDrag(PointerEventData pointer)
	{
		mInputPositionHandler(pointer, TouchPhase.Began);
	}

	public void OnDrag(PointerEventData pointer)
	{
		mInputPositionHandler(pointer, TouchPhase.Moved);
	}

	public void OnEndDrag(PointerEventData pointer)
	{
		mInputPositionHandler(pointer, TouchPhase.Ended);
	}

	public void OnScroll(PointerEventData pointer)
	{
		mScrollHandler(pointer.scrollDelta);
	}


	// Move the scroll according to the dragging position and the dragging state
	private void DragPositionHandler(PointerEventData pointer, TouchPhase state)
	{
		switch (state) {
			case TouchPhase.Began:
				break;

			case TouchPhase.Moved:
				mDeltaInputPosition = GetInputCanvasPosition(pointer.delta);
				// Slide the scroll as long as the moving distance of the pointer
				mMovementCtrl.SetMovement(mDeltaInputPosition, true);
				break;

			case TouchPhase.Ended:
				mMovementCtrl.SetMovement(mDeltaInputPosition / Time.deltaTime, false);
				break;
		}
	}

	// Scroll according to the delta of the mouse scrolling
	private void ScrollDeltaHandler(Vector2 mouseScrollDelta)
	{
		switch (direction) {
			case Direction.Vertical:
				if (mouseScrollDelta.y > 0)
					MoveOneUnitUp();
				else if (mouseScrollDelta.y < 0)
					MoveOneUnitDown();
				break;

			case Direction.Horizontal:
				if (mouseScrollDelta.y > 0)
					MoveOneUnitDown();
				else if (mouseScrollDelta.y < 0)
					MoveOneUnitUp();
				break;
		}
	}

	// Get the input position in the canvas space and return the value of the corresponding axis according to the moving direction.
	private float GetInputCanvasPosition(Vector3 pointerPosition)
	{
		switch (direction) {
			case Direction.Vertical:
				return pointerPosition.y / mParentCanvas.scaleFactor;
			case Direction.Horizontal:
				return pointerPosition.x / mParentCanvas.scaleFactor;
			default:
				return 0.0f;
		}
	}


	// Movement functions 
	// Control the movement of widgets
	private void Update()
	{
		if (!mMovementCtrl.IsMovementEnded()) {
			var distance = mMovementCtrl.GetDistance(Time.deltaTime);
			foreach (PageThreeWidget widget in mWidgets)
                widget.UpdatePosition(distance);
		}
	}

	// Check the status of the scroll
	private void LateUpdate()
	{
		FindDeltaDistanceToCenter();
		if (scrollType == ScrollType.Linear)
			UpdatePositionState();
	}

	// Find the widget which is the closest to the center position,
	// and calculate the delta x or y position between it and the center position.
	private void FindDeltaDistanceToCenter()
	{
		float minDeltaPos = Mathf.Infinity;
		float deltaPos = 0.0f;

		switch (direction) {
			case Direction.Vertical:
				foreach (PageThreeWidget widget in mWidgets) {
					// Skip the disabled widgets in linear mode
					if (!widget.isActiveAndEnabled)
						continue;

					deltaPos = -widget.transform.localPosition.y;
					if (Mathf.Abs(deltaPos) < Mathf.Abs(minDeltaPos))
						minDeltaPos = deltaPos;
				}
				break;

			case Direction.Horizontal:
				foreach (PageThreeWidget widget in mWidgets) {
					// Skip the disabled widgets in linear mode
					if (!widget.isActiveAndEnabled)
						continue;

					deltaPos = -widget.transform.localPosition.x;
					if (Mathf.Abs(deltaPos) < Mathf.Abs(minDeltaPos))
						minDeltaPos = deltaPos;
				}
				break;
		}

		mDeltaDistanceToCenter = minDeltaPos;
	}

	// Move the scoll for the distance of times of unit position
	private void SetUnitMove(int unit)
	{
		mMovementCtrl.SetMovement(unit * unitPos, false);
	}

	// Move all widgets 1 unit up.
	public void MoveOneUnitUp()
	{
		SetUnitMove(1);
	}

	// Move all widgets 1 unit down.
	public void MoveOneUnitDown()
	{
		SetUnitMove(-1);
	}

	// Check if the scroll reaches the end, and store the result to `_isListReachingEnd` this method is used for the linear mode.
	private void UpdatePositionState()
	{
		if (numOfUpperDisabledBoxes >= mMaxNumOfDisabledWidgets &&
		    mDeltaDistanceToCenter > -1e-4)
			mPositionState = PositionState.Top;
		else if (numOfLowerDisabledBoxes >= mMaxNumOfDisabledWidgets &&
		         mDeltaDistanceToCenter < 1e-4)
			mPositionState = PositionState.Bottom;
		else
			mPositionState = PositionState.Middle;
	}

	// Get the object of the centered widget. The centered widget is found by comparing which one is the closest to the center.
	public PageThreeWidget GetCenteredBox()
	{
		float min_position = Mathf.Infinity;
		float position;
		PageThreeWidget candidate_widget = null;

		switch (direction) {
			case Direction.Vertical:
				foreach (PageThreeWidget widget in mWidgets) {
					position = Mathf.Abs(widget.transform.localPosition.y);
					if (position < min_position) {
						min_position = position;
						candidate_widget = widget;
					}
				}
				break;
			case Direction.Horizontal:
				foreach (PageThreeWidget widget in mWidgets) {
					position = Mathf.Abs(widget.transform.localPosition.x);
					if (position < min_position) {
						min_position = position;
						candidate_widget = widget;
					}
				}
				break;
		}

		return candidate_widget;
	}

	//Get the content ID of the centered box
	public int GetCenteredContentID()
	{
		return GetCenteredBox().GetContentID();
	}
}
