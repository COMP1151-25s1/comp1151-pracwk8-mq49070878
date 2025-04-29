using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float damage;
    public float speed;
    public bool isPlayerBullet;

    void Update()
    {
        transform.position -= transform.up * speed * Time.deltaTime;
    }


    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.LogWarning("bullet entered a collider!");

        if (other.tag == "Obstacle")
        {
            Destroy(this.gameObject);
            return;
        }

        if (other.tag == "Player")
        {
            if (isPlayerBullet) return;

            else
            {
                other.GetComponent<PlayerHandler>().TakeDamage(damage);
                Destroy(this.gameObject);
            }
        }
        
        else if (other.GetComponent<EnemyHandler>() != null)
        {
            if (!isPlayerBullet) return;
            
            other.GetComponent<EnemyHandler>().TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
}
