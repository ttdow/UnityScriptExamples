using UnityEngine;

// Unity package for adding the Speech SDK to your Unity project found here:
//https://aka.ms/csspeech/unitypackage
using Microsoft.CognitiveServices.Speech;

public class SpeechHandlerExample : MonoBehaviour
{
    // Your personal Azure Speech Service Subscription Info.
    private const string apiKey = "myAPIkeyhere";
    private const string region = "myregionhere";

    // Holds the configuration info for using Speech SDK.
    private SpeechConfig speechConfig;

    // Concurrency safety variables.
    private object threadLocker = new object();
    private bool waitingForRecord;

    // String containing the speech-to-text result
    public string message;

    public void Start()
    {
        // Pass your API key and region info to the Speech SDK config.
        this.speechConfig = SpeechConfig.FromSubscription(apiKey, region);
    }

    // Call this function on record button press or whatever your use case.
    public async void RecordSpeech()
    {
        // Abort if already recording.
        if (this.waitingForRecord) return;

        using (var recognizer = new SpeechRecognizer(this.speechConfig))
        {
            lock (this.threadLocker) // Thread safety.
            {
                // Flag that we are waiting for a recording to finish.
                this.waitingForRecord = true;
            }

            // Starts speech recognition and returns after speech is finished or 15s passes.
            var result = await recognizer.RecognizeOnceAsync().ConfigureAwait(false);

            // Check result.
            string newMessage = string.Empty;
            if (result.Reason == ResultReason.RecognizedSpeech)
            {
                // Save result from Speech SDK.
                newMessage = result.Text;
            }
            else if (result.Reason == ResultReason.NoMatch)
            {
                Debug.Log("Speech could not be recognized.");
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var error = CancellationDetails.FromResult(result);
                Debug.Log($"ERROR: Reason = {error.Reason}\nErrorDetails = {error.ErrorDetails}.");
            }

            lock (threadLocker) // Thread safety.
            {
                // Save output message.
                this.message = newMessage;

                // Flag that we are no longer waiting for a recording to finish.
                this.waitingForRecord = false;
            }
        }
    }
}
