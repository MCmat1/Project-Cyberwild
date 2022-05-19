using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Gun : MonoBehaviour
{
    [Header("References")]
    [SerializeField] GunData gunData;
    [SerializeField] Transform cam;

    float timeSinceLastShot;

    public TextMeshProUGUI ammoDisplay;
    public TextMeshProUGUI magazineDisplay;

    void Start()
    {
        PlayerShoot.shootInput += Shoot;
        PlayerShoot.reloadInput += StartReload;
    }

    void Update()
    {
        ammoDisplay.text = gunData.currentAmmo.ToString();
        magazineDisplay.text = gunData.magSize.ToString();
        timeSinceLastShot += Time.deltaTime;

        Debug.DrawRay(cam.position, cam.forward);
    }

    bool CanShoot() => !gunData.reloading && timeSinceLastShot > 1f / (gunData.fireRate / 60f);

    public void Shoot()
    {
        if(gunData.currentAmmo > 0)
        {
            if(CanShoot())
            {
                if(Physics.Raycast(cam.position, cam.forward, out RaycastHit hitInfo, gunData.maxDistance))
                {
                    IDamageable damageable = hitInfo.transform.GetComponent<IDamageable>();
                    damageable?.TakeDamage(gunData.damage);
                }
            
            gunData.currentAmmo--;
            timeSinceLastShot = 0;
            OnGunShot();
            }
        }
    }

    void OnDisable() => gunData.reloading = false;

    void OnGunShot()
    {
        
    }

    void StartReload()
    {
        if(!gunData.reloading && this.gameObject.activeSelf)
        {
            StartCoroutine(Reload());
        }
    }

    IEnumerator Reload()
    {
        gunData.reloading = true;

        yield return new WaitForSeconds(gunData.reloadTime);

        gunData.currentAmmo = gunData.magSize;

        gunData.reloading = false;
    }    
}
