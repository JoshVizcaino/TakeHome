using AnimationCurveExtend;
using System;
using UnityEngine;
using PositionState = ScrollPositionCtrl.PositionState;

public interface IMovementCtrl
{
	void SetMovement(float baseValue, bool flag);
	bool IsMovementEnded();
	float GetDistance(float deltaTime);
}

//Control the movement for the free movement
// There are three status of the movement:
// - Dragging: The moving distance is the same as the dragging distance
// - Released: When the scroll is released after being dragged, the moving distance is decided by the releasing velocity and a velocity factor curve
// - Aligning: If the aligning option is set or the scroll reaches the end in the linear mode, the movement will switch to this status to make the scroll move to the desired position.
public class FreeMovementCtrl : IMovementCtrl
{
	// The movement for the free movement
	private readonly VelocityMovement mReleasingMovement;
	// The movement for aligning the list
	private readonly DistanceMovement mAligningMovement;
	// Is the list being dragged?
	private bool mIsDragging;
	// The dragging distance
	private float mDraggingDistance;
	// Does it need to align the list after a movement?
	private readonly bool mToAlign;
	// How far does the list exceed the end
	private float mOverGoingDistance;
	// How far could the 1ist exceed the end?
	private readonly float mOverGoingDistanceThreshold;
	// The velocity threshold that stop the scroll to align it. It is used when `alignMiddle` is true.
	private const float mStopVelocityThreshold = 200.0f;
	// The function that calculating the distance to align the scroll
	private readonly Func<float> getAligningDistance;
	// The function that getting the state of the scroll position
	private readonly Func<PositionState> getPositionState;

	//Constructor
	// @param releasingCurve The curve that defines the velocity factor for the releasing movement. The x axis is the moving duration, and the y axis is the factor.
	// @param toAlign Does it need to align after a movement?
	// @param overGoingDistanceThreshold How far could the scroll exceed the end?
	// @param inGetAligningDistance The function that evaluates the distance for aligning
	// @param inGetPositionState The function that returns the state of the scroll position
	public FreeMovementCtrl(AnimationCurve releasingCurve, bool toAlign,
		float overGoingDistanceThreshold,
		Func<float> inGetAligningDistance, Func<PositionState> inGetPositionState)
	{
		mReleasingMovement = new VelocityMovement(releasingCurve);
		mAligningMovement = new DistanceMovement(
			AnimationCurve.EaseInOut(0.0f, 0.0f, 0.25f, 1.0f));
		mToAlign = toAlign;
		mOverGoingDistanceThreshold = overGoingDistanceThreshold;
		getAligningDistance = inGetAligningDistance;
		getPositionState = inGetPositionState;
	}

	// Set the base value for this new movement
	// @param value If `isDragging` is true, this value is the dragging distance. Otherwise, this value is the base velocity for the releasing movement.
	// @param isDragging Is the scroll being dragged?
	public void SetMovement(float value, bool isDragging)
	{
		if (isDragging) {
			mIsDragging = true;
			mDraggingDistance = value;

			// End the last releasing movement when start dragging
			if (!mReleasingMovement.IsMovementEnded())
				mReleasingMovement.EndMovement();
		} else if (getPositionState() != PositionState.Middle) {
			mAligningMovement.SetMovement(getAligningDistance());
		} else {
			mReleasingMovement.SetMovement(value);
		}
	}

	// Is the movement ended?
	public bool IsMovementEnded()
	{
		return !mIsDragging &&
		       mAligningMovement.IsMovementEnded() &&
		       mReleasingMovement.IsMovementEnded();
	}

	// Get moving distance in the given delta time
	public float GetDistance(float deltaTime)
	{
		var distance = 0.0f;

		// If it's dragging, return the dragging distance set from `SetMovement()`
		if (mIsDragging) {
			mIsDragging = false;
			distance = mDraggingDistance;

			if (IsGoingTooFar(mDraggingDistance)) {
				var threshold = mOverGoingDistanceThreshold * Mathf.Sign(mOverGoingDistance);
				distance -= mOverGoingDistance - threshold;
			}
		}
		// Aligning
		else if (!mAligningMovement.IsMovementEnded()) {
			distance = mAligningMovement.GetDistance(deltaTime);
		}
		// Releasing
		else if (!mReleasingMovement.IsMovementEnded()) {
			distance = mReleasingMovement.GetDistance(deltaTime);

			if (NeedToAlign(distance)) {
				// Make the releasing movement end
				mReleasingMovement.EndMovement();

				// Start the aligning movement instead
				mAligningMovement.SetMovement(getAligningDistance());
				distance = mAligningMovement.GetDistance(deltaTime);
			}
		}

		return distance;
	}

	// Check whether it needs to switch to the aligning movement or not
	// Return true if the scroll reaches the end and it exceeds the end for a distance, or if the aligning mode is on and the scroll moves too slow.
	private bool NeedToAlign(float distance)
	{
		return IsGoingTooFar(distance) ||
		       (mToAlign &&
		        Mathf.Abs(mReleasingMovement.lastVelocity) < mStopVelocityThreshold);
	}

	private bool IsGoingTooFar(float distance)
	{
		if (getPositionState() == PositionState.Middle)
			return false;

		mOverGoingDistance = -1 * getAligningDistance();
		return Mathf.Abs(mOverGoingDistance += distance) > mOverGoingDistanceThreshold;
	}
}

// Control the movement for the unit movement
// It is controlled by the distance movement which moves for the given distance.
// If the list reaches the end in the linear mode, it will controlled by the bouncing movement which performs a back and forth movement.
public class UnitMovementCtrl : IMovementCtrl
{
	// The movement for the unit movement
	private readonly DistanceMovement mUnitMovement;
	// The movement for the bouncing movement when the scroll reaches the end
	private readonly DistanceMovement mBouncingMovement;
	// The delta position for the bouncing effect
	private readonly float mBouncingDeltaPos;
	// The function that returns the distance for aligning
	private readonly Func<float> getAligningDistance;
	// The function that returns the state of the scroll position
	private readonly Func<PositionState> getPositionState;

	 // Constructor
	 // @param movementCurve The curve that defines the distance factor. The x axis is the moving duration, and y axis is the factor value.
	 // @param bouncingDeltaPos The delta position for bouncing effect
	 // @param getAligningDistance The function that evaluates the distance for aligning
	 // @param getPositionState The function that returns the state of the list position
	public UnitMovementCtrl(AnimationCurve movementCurve, float bouncingDeltaPos,
		Func<float> inGetAligningDistance, Func<PositionState> inGetPositionState)
	 {
		var bouncingCurve = new AnimationCurve(
			new Keyframe(0.0f, 0.0f, 0.0f, 5.0f),
			new Keyframe(0.125f, 1.0f, 0.0f, 0.0f),
			new Keyframe(0.25f, 0.0f, -5.0f, 0.0f));

		mUnitMovement = new DistanceMovement(movementCurve);
		mBouncingMovement = new DistanceMovement(bouncingCurve);
		mBouncingDeltaPos = bouncingDeltaPos;
		getAligningDistance = inGetAligningDistance;
		getPositionState = inGetPositionState;
	 }

	 // Set the moving distance for this new movement
	 // If there has the distance left in the last movement, the moving distance will be accumulated.
	 // If the scroll reaches the end in the linear mode, the moving distance will be ignored and use `mBouncingDeltaPos` for the bouncing movement.
	public void SetMovement(float distanceAdded, bool flag)
	{
		// Ignore any movement when the scroll is aligning
		if (!mBouncingMovement.IsMovementEnded())
			return;

		var state = getPositionState();
		var movingDirection = Mathf.Sign(distanceAdded);

		if ((state == PositionState.Top && movingDirection < 0) ||
		    (state == PositionState.Bottom && movingDirection > 0)) {
			mBouncingMovement.SetMovement(movingDirection * mBouncingDeltaPos);
		} else {
			distanceAdded += mUnitMovement.distanceRemaining;
			mUnitMovement.SetMovement(distanceAdded);
		}
	}

	// Is the movement ended?
	public bool IsMovementEnded()
	{
		return mBouncingMovement.IsMovementEnded() &&
		       mUnitMovement.IsMovementEnded();
	}

	// Get the moving distance in the given delta time
	public float GetDistance(float deltaTime)
	{
		var distance = 0.0f;

		if (!mBouncingMovement.IsMovementEnded()) {
			distance = mBouncingMovement.GetDistance(deltaTime);
		} else {
			distance = mUnitMovement.GetDistance(deltaTime);

			if (NeedToAlign(distance)) {
				// Make the unit movement end
				mUnitMovement.EndMovement();

				mBouncingMovement.SetMovement(-1 * getAligningDistance());
				// Start at the furthest point to move back
				mBouncingMovement.GetDistance(0.125f);
				distance = mBouncingMovement.GetDistance(deltaTime);
			}
		}

		return distance;
	}

	// Check whether it needs to switch to the aligning mode
	// Return true if the scroll exceeds the end for a distance or the unit movement is ended.
	private bool NeedToAlign(float deltaDistance)
	{
		if (getPositionState() == PositionState.Middle)
			return false;

		return Mathf.Abs(getAligningDistance() * -1 + deltaDistance) > mBouncingDeltaPos ||
		        mUnitMovement.IsMovementEnded();
	}
}

// Evaluate the moving distance within the given delta time according to the velocity factor curve
internal class VelocityMovement
{
	 // The curve that evaluating the velocity factor at the accumulated delta time
	 // The evaluated value will be multiplied by the `mBaseVelocity` to get the final velocity.
	private readonly DeltaTimeCurve mVelocityFactorCurve;
	// The referencing velocity for a movement
	private float mBaseVelocity;
	// The velocity at the last `GetDistance()` call or the last `SetMovement()` call
	public float lastVelocity { get; private set; }

	 //Constructor
	 // @param factorCurve The curve that defines the velocity factor. The x axis is the moving duration, and the y axis is the factor.
	public VelocityMovement(AnimationCurve factorCurve)
	{
		mVelocityFactorCurve = new DeltaTimeCurve(factorCurve);
	}

	// Set the base velocity for this new movement
	public void SetMovement(float baseVelocity)
	{
		mVelocityFactorCurve.Reset();
		mBaseVelocity = baseVelocity;
		lastVelocity = mVelocityFactorCurve.CurrentEvaluate() * mBaseVelocity;
	}

	// Is the movement ended?
	public bool IsMovementEnded()
	{
		return mVelocityFactorCurve.IsTimeOut();
	}

	// Forcibly end the movement by making it time out
	public void EndMovement()
	{
		mVelocityFactorCurve.Evaluate(mVelocityFactorCurve.timeTotal);
	}

	 // Get moving distance in the given delta time
	 // The given delta time will be accumulated first, and then get the velocity at the accumulated time. The velocity will be multiplied by the given delta time to get the moving distance.
	public float GetDistance(float deltaTime)
	{
		lastVelocity = mVelocityFactorCurve.Evaluate(deltaTime) * mBaseVelocity;
		return lastVelocity * deltaTime;
	}
}

// Evaluate the moving distance within the given delta time according to the total moving distance
internal class DistanceMovement
{
	// The curve that evaluating the distance factor at the accumulated delta time
	// The evaluated value will be multiplied by `mDistanceTotal` to get the final moving distance.
	private readonly DeltaTimeCurve mDistanceFactorCurve;
	// The total moving distance in a movement
	private float mDistanceTotal;
	// The last target distance in a movement
	private float mLastDistance;
	// The remaining moving distance in a movement
	public float distanceRemaining
	{
		get { return mDistanceTotal - mLastDistance; }
	}

	// Constructor
	// @param factorCurve The curve that defines the distance factor. The x axis is the moving duration, and y axis is the factor value.
	public DistanceMovement(AnimationCurve factorCurve)
	{
		mDistanceFactorCurve = new DeltaTimeCurve(factorCurve);
	}

	// Set the moving distance for this new movement
	public void SetMovement(float totalDistance)
	{
		mDistanceFactorCurve.Reset();
		mDistanceTotal = totalDistance;
		mLastDistance = 0.0f;
	}

	public bool IsMovementEnded()
	{
		return mDistanceFactorCurve.IsTimeOut();
	}

	// Forcibly end the movement by making it time out
	public void EndMovement()
	{
		mDistanceFactorCurve.Evaluate(mDistanceFactorCurve.timeTotal);
		mLastDistance = mDistanceTotal;
	}

	// Get the moving distance in the given delta time
	// The time will be accumulated first, and then get the final distance at the time accumulated, and subtract it from the passed distance to get the moving distance in the given delta time.
	public float GetDistance(float deltaTime)
	{
		var nextDistance = mDistanceTotal * mDistanceFactorCurve.Evaluate(deltaTime);
		var deltaDistance = nextDistance - mLastDistance;

		mLastDistance = nextDistance;
		return deltaDistance;
	}
}
