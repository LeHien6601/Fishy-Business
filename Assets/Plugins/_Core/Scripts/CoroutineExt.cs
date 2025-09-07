using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// NMHung - hung.nguyencshcmut
/// </summary>
public static class CoroutineExt
{
    public static IEnumerator WaitThenExecute(float wait, Action callback)
    {
        yield return new WaitForSeconds(wait);
        callback();
    }
    public static IEnumerator WaitThenExecute(this MonoBehaviour owner, float wait, Action callback)
    {
        IEnumerator coroutine = WaitThenExecute(wait, callback);
        owner.StartCoroutine(coroutine);
        return coroutine;
    }


    private static IEnumerator WaitOneFrame(Action callback)
    {
        yield return null;
        callback();
    }
    public static IEnumerator WaitOneFrame(this MonoBehaviour owner, Action callback)
    {
        IEnumerator coroutine = WaitOneFrame(callback);
        owner.StartCoroutine(coroutine);
        return coroutine;
    }


    private static IEnumerator WaitForCondition<T>(Predicate<T> condition, T parameter, Action<T> callback, Action<T> onWait, float interval = 0.1f)
    {
        WaitForSeconds wait = new(interval);
        while (!condition(parameter))
        {
            onWait?.Invoke(parameter);
            yield return wait;
        }
        callback(parameter);
    }

    private static IEnumerator WaitForCondition<T>(Predicate<T> condition, T parameter, Action<T> callback, float interval = 0.1f)
    {
        WaitForSeconds wait = new(interval);
        while (!condition(parameter))
        {
            yield return wait;
        }
        callback(parameter);
    }

    private static IEnumerator WaitForCondition(Func<bool> condition, Action callback, float interval = 0.1f)
    {
        WaitForSeconds wait = new(interval);
        while (!condition())
        {
            yield return wait;
        }
        callback();
    }

    private static IEnumerator WaitForCondition(Func<bool> condition, Action callback, Action onWait, float interval = 0.1f)
    {
        WaitForSeconds wait = new(interval);
        while (!condition())
        {
            onWait?.Invoke();
            yield return wait;
        }
        callback();
    }

    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that waits until a condition is met, 
    /// checking at specified intervals, then executes a callback with a parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter passed to the condition and callback.</typeparam>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="condition">Predicate to check the condition.</param>
    /// <param name="parameter">Parameter to pass to the condition and callback.</param>
    /// <param name="callback">Action to execute when the condition is met.</param>
    /// <param name="onWait">Optional action to call each time the condition is checked and not met.</param>
    /// <param name="interval">Time to wait between condition checks (in seconds). Defaults to 0.1f.</param>
    public static void WaitForCondition<T>(this MonoBehaviour owner, Predicate<T> condition, T parameter, Action<T> callback, Action<T> onWait, float interval = 0.1f)
        => owner.StartCoroutine(WaitForCondition(condition, parameter, callback, onWait, interval));

    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that waits until a condition is met, 
    /// checking at specified intervals, then executes a callback with a parameter.
    /// </summary>
    /// <typeparam name="T">Type of the parameter passed to the condition and callback.</typeparam>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="condition">Predicate to check the condition.</param>
    /// <param name="parameter">Parameter to pass to the condition and callback.</param>
    /// <param name="callback">Action to execute when the condition is met.</param>
    /// <param name="interval">Time to wait between condition checks (in seconds). Defaults to 0.1f.</param>
    public static void WaitForCondition<T>(this MonoBehaviour owner, Predicate<T> condition, T parameter, Action<T> callback, float interval = 0.1f)
        => owner.StartCoroutine(WaitForCondition(condition, parameter, callback, interval));

    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that waits until a condition is met, 
    /// checking at specified intervals, then executes a callback.
    /// </summary>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="condition">Function to check the condition.</param>
    /// <param name="callback">Action to execute when the condition is met.</param>
    /// <param name="interval">Time to wait between condition checks (in seconds). Defaults to 0.1f.</param>
    public static void WaitForCondition(this MonoBehaviour owner, Func<bool> condition, Action callback, float interval = 0.1f)
        => owner.StartCoroutine(WaitForCondition(condition, callback, interval));

    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that waits until a condition is met, 
    /// checking at specified intervals, then executes a callback, with an optional action 
    /// called each time the condition is checked and not met.
    /// </summary>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="condition">Function to check the condition.</param>
    /// <param name="callback">Action to execute when the condition is met.</param>
    /// <param name="onWait">Optional action to call each time the condition is checked and not met.</param>
    /// <param name="interval">Time to wait between condition checks (in seconds). Defaults to 0.1f.</param>
    public static void WaitForCondition(this MonoBehaviour owner, Func<bool> condition, Action callback, Action onWait, float interval = 0.1f)
        => owner.StartCoroutine(WaitForCondition(condition, callback, onWait, interval));



    private static IEnumerator LoopWithInterval(Action callback, float interval, int numberOfLoops, Func<bool> stopCondition)
    {
        if (callback == null)
        {
            Debug.LogError("Callback is null in LoopWithInterval.");
            yield break;
        }
        if (interval <= 0f)
        {
            Debug.LogError("Interval must be positive in LoopWithInterval.");
            yield break;
        }

        int loopCount = 0;
        while (numberOfLoops < 0 || loopCount < numberOfLoops)
        {
            if (stopCondition != null && stopCondition())
            {
                break;
            }
            callback();
            loopCount++;
            yield return new WaitForSeconds(interval);
        }
    }

    private static IEnumerator LoopWithInterval(Action callback, float interval)
    {
        if (callback == null)
        {
            Debug.LogError("Callback is null in LoopWithInterval.");
            yield break;
        }
        if (interval <= 0f)
        {
            Debug.LogError("Interval must be positive in LoopWithInterval.");
            yield break;
        }

        while (true)
        {
            callback();
            yield return new WaitForSeconds(interval);
        }
    }
    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that executes a callback repeatedly, 
    /// waiting for a specified interval between each execution, for a given number of loops 
    /// or until an optional stop condition is met.
    /// </summary>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="callback">Action to execute each loop.</param>
    /// <param name="interval">Time to wait between each loop (in seconds).</param>
    /// <param name="numberOfLoops">Number of times to loop. Use -1 for infinite looping.</param>
    /// <param name="stopCondition">Optional condition that, when true, stops the loop early. Defaults to null.</param>
    public static IEnumerator LoopWithInterval(this MonoBehaviour owner, Action callback, float interval, int numberOfLoops, Func<bool> stopCondition = null)
    {
        IEnumerator coroutine = LoopWithInterval(callback, interval, numberOfLoops, stopCondition);
        owner.StartCoroutine(coroutine);
        return coroutine;
    }

    /// <summary>
    /// Starts a coroutine on the specified MonoBehaviour that executes a callback repeatedly, 
    /// waiting for a specified interval between each execution, for a given number of loops 
    /// or until an optional stop condition is met.
    /// </summary>
    /// <param name="owner">The MonoBehaviour that will run the coroutine.</param>
    /// <param name="callback">Action to execute each loop.</param>
    /// <param name="interval">Time to wait between each loop (in seconds).</param>
    public static IEnumerator LoopWithInterval(this MonoBehaviour owner, Action callback, float interval)
    {
        IEnumerator coroutine = LoopWithInterval(callback, interval);
        owner.StartCoroutine(coroutine);
        return coroutine;
    }

    private static IEnumerator RepeatEachFrame(int count, Action callback, Action onComplete)
    {
        while (count-- > 0)
        {
            callback();
            yield return null;
        }
        onComplete?.Invoke();
    }

    public static IEnumerator RepeatEachFrame(this MonoBehaviour owner, int count, Action callback, Action onComplete = null)
    {
        IEnumerator coroutine = RepeatEachFrame(count, callback, onComplete);
        owner.StartCoroutine(coroutine);
        return coroutine;
    }

}