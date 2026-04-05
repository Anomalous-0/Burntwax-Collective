using System.IO;
using UnityEngine;

public class GunSoundGenerator : MonoBehaviour
{
    public int sampleRate = 44100;

    void Start()
    {
        Directory.CreateDirectory(Application.dataPath + "/GeneratedAudio");

        GenerateSet("pistol", GeneratePistol);
        GenerateSet("smg", GenerateSMG);
        GenerateSet("shotgun", GenerateShotgun);
        GenerateSet("revolver", GenerateRevolver);

        Debug.Log("Gun sounds generated!");
    }

    void GenerateSet(string name, System.Func<float[], float[]> generator)
    {
        for (int i = 1; i <= 3; i++)
        {
            float[] samples = new float[sampleRate / 3];
            samples = generator(samples);

            string path = Application.dataPath + $"/GeneratedAudio/{name}_{i}.wav";
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

    // 🔫 PISTOL
    float[] GeneratePistol(float[] data)
    {
        float freq = Random.Range(1500f, 2500f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;

            data[i] =
                0.7f * Mathf.Sin(2 * Mathf.PI * freq * t) +
                0.4f * Random.Range(-1f, 1f);
        }

        return Envelope(data, 14f);
    }

    // 🔫 SMG (lighter, sharper)
    float[] GenerateSMG(float[] data)
    {
        float freq = Random.Range(2000f, 3500f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;

            data[i] =
                0.5f * Mathf.Sin(2 * Mathf.PI * freq * t) +
                0.6f * Random.Range(-1f, 1f);
        }

        return Envelope(data, 18f);
    }

    // 💥 SHOTGUN (heavy low-end)
    float[] GenerateShotgun(float[] data)
    {
        float freq = Random.Range(80f, 150f);

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;

            data[i] =
                1.0f * Mathf.Sin(2 * Mathf.PI * freq * t) +
                0.8f * Random.Range(-1f, 1f);
        }

        return Envelope(data, 8f);
    }

    // 🔥 REVOLVER (punchy + metallic)
    float[] GenerateRevolver(float[] data)
    {
        float freq1 = Random.Range(1200f, 2000f);
        float freq2 = freq1 * 1.8f;

        for (int i = 0; i < data.Length; i++)
        {
            float t = i / (float)sampleRate;

            data[i] =
                0.8f * Mathf.Sin(2 * Mathf.PI * freq1 * t) +
                0.4f * Mathf.Sin(2 * Mathf.PI * freq2 * t) +
                0.4f * Random.Range(-1f, 1f);
        }

        return Envelope(data, 10f);
    }

    void SaveWav(string path, float[] samples)
    {
        using (FileStream fs = new FileStream(path, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int sampleCount = samples.Length;
            int byteRate = sampleRate * 2;

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