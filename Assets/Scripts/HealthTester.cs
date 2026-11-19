using UnityEngine;
using UnityEngine.InputSystem; // Añadimos la librería del nuevo sistema

[RequireComponent(typeof(HealthSystem))]
public class HealthTester : MonoBehaviour
{
    private HealthSystem healthSystem;

    private void Start()
    {
        healthSystem = GetComponent<HealthSystem>();
    }

    private void Update()
    {
        // Verificamos si hay un teclado conectado para evitar errores
        if (Keyboard.current == null) return;

        // Presiona la barra espaciadora para recibir daño
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            healthSystem.TakeDamage(15);
            Debug.Log("Daño recibido. Vida actual: " + healthSystem.CurrentHealth);
        }

        // Presiona la tecla H para curar
        if (Keyboard.current.hKey.wasPressedThisFrame)
        {
            healthSystem.Heal(10);
            Debug.Log("Curación recibida. Vida actual: " + healthSystem.CurrentHealth);
        }
    }
}