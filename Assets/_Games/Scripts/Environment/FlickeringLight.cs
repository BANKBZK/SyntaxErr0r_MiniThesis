using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))] // บังคับว่าสคริปต์นี้ต้องแปะอยู่กับ Light เท่านั้น
public class FlickeringLight : MonoBehaviour
{
    public enum FlickerMode
    {
        InstantOnOff, // ติด/ดับ แบบทันที (0, 1)
        SmoothFade,   // ค่อยๆ หรี่และสว่างสลับกัน
        HorrorFlicker // กระพริบถี่ๆ แบบไฟช็อต/ไฟใกล้พัง
    }

    [Header("Flicker Settings")]
    [Tooltip("เลือกรูปแบบการกระพริบ")]
    public FlickerMode mode = FlickerMode.HorrorFlicker;

    [Tooltip("เวลาสุ่มน้อยสุดที่จะเปลี่ยนสถานะไฟ (วินาที)")]
    public float minInterval = 0.1f;
    [Tooltip("เวลาสุ่มมากสุดที่จะเปลี่ยนสถานะไฟ (วินาที)")]
    public float maxInterval = 0.5f;

    [Header("Intensity Limits")]
    [Tooltip("ความสว่างน้อยสุด (0 = มืดสนิท)")]
    public float minIntensity = 0f;
    [Tooltip("ความสว่างมากสุด (ถ้าปล่อย 0 มันจะดึงค่าตั้งต้นของหลอดไฟมาใช้)")]
    public float maxIntensity = 0f;

    [Header("Smooth Mode Settings")]
    [Tooltip("ความเร็วในการหรี่ไฟ (ใช้เฉพาะโหมด SmoothFade)")]
    public float fadeSpeed = 3f;

    private Light _light;
    private float _targetIntensity;
    private float _baseIntensity;

    private void Start()
    {
        _light = GetComponent<Light>();
        _baseIntensity = _light.intensity;

        // ถ้าไม่ได้ตั้งค่า Max ไว้ ให้เอาค่าความสว่างตั้งต้นของหลอดไฟมาใช้
        if (maxIntensity <= 0f)
        {
            maxIntensity = _baseIntensity;
        }

        StartCoroutine(FlickerRoutine());
    }

    private void Update()
    {
        // โหมด SmoothFade ต้องใช้ Update ช่วยค่อยๆ เลื่อนค่าแสงให้สมูท
        if (mode == FlickerMode.SmoothFade)
        {
            _light.intensity = Mathf.Lerp(_light.intensity, _targetIntensity, Time.deltaTime * fadeSpeed);
        }
    }

    private IEnumerator FlickerRoutine()
    {
        while (true)
        {
            // สุ่มเวลาที่จะรอในรอบถัดไป
            float waitTime = Random.Range(minInterval, maxInterval);

            switch (mode)
            {
                case FlickerMode.InstantOnOff:
                    // สลับไปมาแค่มืดสุด กับ สว่างสุด
                    _light.intensity = _light.intensity > minIntensity ? minIntensity : maxIntensity;
                    yield return new WaitForSeconds(waitTime);
                    break;

                case FlickerMode.SmoothFade:
                    // สุ่มเป้าหมายความสว่างใหม่ แล้วปล่อยให้ Update ค่อยๆ เลื่อนค่า (Lerp) ไปหา
                    _targetIntensity = Random.Range(minIntensity, maxIntensity);
                    yield return new WaitForSeconds(waitTime);
                    break;

                case FlickerMode.HorrorFlicker:
                    // กระพริบสุ่มมั่วๆ แบบไฟพัง (รอเวลาน้อยมากๆ เพื่อให้มันถี่)
                    _light.intensity = Random.Range(minIntensity, maxIntensity);
                    yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
                    break;
            }
        }
    }
}