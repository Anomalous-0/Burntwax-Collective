using System.IO;
using UnityEngine;

public class BulletImpactSoundGenerator : MonoBehaviour
{
    public int sampleRate = 44100;

    void Start()
    {
        Directory.CreateDirectory(Application.dataPath + "/GeneratedAudio");

        GenerateSet("glass", GenerateGlass);
        GenerateSet("sand", GenerateSand);
        GenerateSet("concrete", GenerateConcrete);
        GenerateSet("dirt", GenerateDirt);
        GenerateSet("metal", GenerateMetal);

        Debug.Log("All sounds generated!");
    }

    void GenerateSet(string name, System.Func<float[], float[]> generator)
    {
        for (int i = 1; i <= 3; i++)
        {
            float[] samples = new float[sampleRate / 4];
            samples = generator(samples);

            string path = Application.dataPath + $"/GeneratedAudio/bullet_{name}_{i}.wav";
            SaveWav(path, samples);
        }
    }

    float[] Envelope(float[] data, float decay)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float t = (float)i / data.Length;
            data[i] *= Mathf.Exp(-decay * t);
        }
        return data;
    }

    float[] GenerateGlass(float[] data)
    {
        float freq1 = Random.Range(2500f, 4000f);
        float freq2 = Random.Range(4000f, 6000f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;
            data[i] =
                0.6f * Mathf.Sin(2 * Mathf.PI * freq1 * t) +
                0.3f * Mathf.Sin(2 * Mathf.PI * freq2 * t) +
                0.2f * Random.Range(-1f, 1f);
        }
        return Envelope(data, 15f);
    }

    float[] GenerateSand(float[] data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = 0.3f * Random.Range(-1f, 1f);
        }
        return Envelope(data, 25f);
    }

    float[] GenerateConcrete(float[] data)
    {
        float freq = Random.Range(800f, 1500f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;
            data[i] =
                0.5f * Mathf.Sin(2 * Mathf.PI * freq * t) +
                0.5f * Random.Range(-1f, 1f);
        }
        return Envelope(data, 12f);
    }

    float[] GenerateDirt(float[] data)
    {
        float freq = Random.Range(200f, 400f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;
            data[i] =
                0.3f * Mathf.Sin(2 * Mathf.PI * freq * t) +
                0.5f * Random.Range(-1f, 1f);
        }
        return Envelope(data, 20f);
    }

    float[] GenerateMetal(float[] data)
    {
        float freq1 = Random.Range(1500f, 2500f);
        float freq2 = freq1 * 2f;

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;
            data[i] =
                Mathf.Sin(2 * Mathf.PI * freq1 * t) +
                0.5f * Mathf.Sin(2 * Mathf.PI * freq2 * t);
        }
        return Envelope(data, 6f);
    }

    void SaveWav(string path, float[] samples)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int sampleCount = samples.Length;
            int byteRate = sampleRate * 2;

            // WAV HEADER
            bw.Write(System.Text.Encoding.UTF8.GetBytes("RIFF"));
            bw.Write(36 + sampleCount * 2);
            bw.Write(System.Text.Encoding.UTF8.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.UTF8.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.UTF8.GetBytes("data"));
            bw.Write(sampleCount * 2);

            foreach (var sample in samples)
            {
                short s = (short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue);
                bw.Write(s);
            }
        }
    }
}