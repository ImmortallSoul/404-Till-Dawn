using UnityEngine;

public class DamageNumber : MonoBehaviour
{
    static GUIStyle _style;

    string _text;
    Color _color;
    float _life;
    Vector3 _velocity;

    public static void Spawn(Vector3 world, float amount, bool crit)
    {
        var go = new GameObject("DamageNumber");
        go.transform.position = world;
        var number = go.AddComponent<DamageNumber>();
        number._text = crit ? Mathf.RoundToInt(amount) + "!" : Mathf.RoundToInt(amount).ToString();
        number._color = crit ? new Color(1f, 0.85f, 0.25f) : new Color(0.9f, 0.95f, 1f);
        number._life = crit ? 0.9f : 0.65f;
        number._velocity = new Vector3(Random.Range(-0.5f, 0.5f), 1.7f, 0f);
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        _life -= dt;
        if (_life <= 0f)
        {
            Destroy(gameObject);
            return;
        }
        transform.position += _velocity * dt;
        _velocity.y -= 1.5f * dt;
    }

    void OnGUI()
    {
        if (Camera.main == null)
            return;
        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }

        Vector3 screen = Camera.main.WorldToScreenPoint(transform.position);
        if (screen.z <= 0f)
            return;

        Color old = GUI.color;
        GUI.color = _color;
        GUI.Label(new Rect(screen.x - 40f, Screen.height - screen.y - 12f, 80f, 24f), _text, _style);
        GUI.color = old;
    }
}
