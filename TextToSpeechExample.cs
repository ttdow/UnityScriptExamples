using System;
using System.Threading;

using UnityEngine;

// Unity package for adding the Speech SDK to your Unity project found here:
//https://aka.ms/csspeech/unitypackage
using Microsoft.CognitiveServices.Speech;

public class TextToSpeechExample : MonoBehaviour
{
    // Speech synthesis variables
    private const string _subscriptionKey = "myAPIkeyhere";
    private const string _region = "myregionhere";

    // Audio sample rate.
    private const int sampleRate = 24000;

    // Holds the configuration info for using Speech SDK.
    private SpeechConfig speechConfig;

    // Speech SDK object used for text-to-speech.
    private SpeechSynthesizer synthesizer;

    // Concurrency safety variables.
    private object threadLocker = new object();
    public bool waitingForSpeak;

    // Output audio source variables.
    public AudioSource audioSource;

    public void Start()
    {
        //  Setup text-to-speech with Speech SDK.
        this.speechConfig = SpeechConfig.FromSubscription(_subscriptionKey, _region);
        this.speechConfig.SetSpeechSynthesisOutputFormat(SpeechSynthesisOutputFormat.Raw24Khz16BitMonoPcm);

        // Setup synthesizer.
        this.synthesizer = new SpeechSynthesizer(speechConfig, null);

        // Setup synthesizer errors.
        this.synthesizer.SynthesisCanceled += (s, e) =>
        {
            var cancellation = SpeechSynthesisCancellationDetails.FromResult(e.Result);
            Debug.Log($"CANCELED: Reason = {cancellation.Reason}\nErrorDetails = {cancellation.ErrorDetails}");
        };
    }

    // Call this function and pass your text to turn into speech.
    public void Speak(string textToSay)
    {
        lock (this.threadLocker) // Thread safety.
        {
            // Flag we are waiting for audio (speech) to finish generating.
            this.waitingForSpeak = true;
        }

        // Used to time operation (there is a slight delay).
        var startTime = DateTime.Now;

        // Tell synthesizer to convert the string to audio (speech).
        using (var result = synthesizer.StartSpeakingTextAsync(textToSay).Result)
        {
            var audioDataStream = AudioDataStream.FromResult(result);
            var isFirstAudioChunk = true;
            var audioClip = AudioClip.Create
            (
                "Speech",               // Name.
                sampleRate * 600,       // Sample length.
                1,                      // Audio channels.
                sampleRate,             // Sample rate.
                true,                   // Stream audio data.
                (float[] audioChunk) => // Callback function for reading PCM audio file.
                {
                    var chunkSize = audioChunk.Length;
                    var audioChunkBytes = new byte[chunkSize * 2];
                    var readBytes = audioDataStream.ReadData(audioChunkBytes);

                    if (isFirstAudioChunk && readBytes > 0)
                    {
                        var endTime = DateTime.Now;
                        var latency = endTime.Subtract(startTime).TotalMilliseconds;
                        Debug.Log($"Speech synthesis succeeded! Latency = {latency} ms.");
                        isFirstAudioChunk = false;
                    }

                    for (int i = 0; i < chunkSize; i++)
                    {
                        if (i < readBytes / 2)
                        {
                            audioChunk[i] = (short)(audioChunkBytes[i * 2 + 1] << 8 | audioChunkBytes[i * 2]) / 32768.0F;
                        }
                        else
                        {
                            audioChunk[i] = 0.0f;
                        }
                    }

                    if (readBytes == 0)
                    {
                        Thread.Sleep(200); // Small pause to ensure clip is ready to play.
                    }
                }
            );

            // Pass clip to audio source var and play it.
            this.audioSource.clip = audioClip;
            this.audioSource.Play();
        }

        lock (this.threadLocker) // Thread safety.
        {
            // Flag audio (speech) is generated.
            this.waitingForSpeak = false;
        }
    }
}
