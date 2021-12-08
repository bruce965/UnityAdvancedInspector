Unity Advanced Inspector
========================

Inspector augmenting utilities for Unity.


## Installation

Window » Package Manager » _"+"_ button in the corner »
Add package from Git URL... »
`https://github.com/bruce965/UnityAdvancedInspector.git` » Add


## Usage

This package provides various tools and features.

> Warning: this package's public API is still a work-in-progress, it might change
> in the future.

### Advanced Inspector

Adding the `[AdvancedInspector]` type to a class will replace Unity's default
inspector with the advanced inspector provided in this package.

Otherwise, it's possible to use `[AdvancedInspector(DefaultInspector=true)]` to
render Unity's default inspector, followed by the advanced inspector provided in
this package.

Field properties can be configured through the `[InspectorField]` attribute.

```csharp
using UnityAdvancedInspector;
using UnityEngine;

[AdvancedInspector]
public class Player : MonoBehaviour
{
    // implicit property backing field will be serialized by Unity
    [InspectorField]
    public float BaseHealth { get; set; } = 100f;

    // private field with [SerializeField] attribute will be serialized by Unity
    // but since there is no [InspectorField], it will not be shown in inspector
    [SerializeField]
    float _healthMultiplier = 2f;

    // no backing field, will not be serialized by Unity
    [InspectorField(Label="Health (total)")]
    public float TotalHealth
    {
        get => BaseHealth * _healthMultiplier;
        set => BaseHealth = value / _healthMultiplier;
    }

    // implicit property backing field will be serialized by Unity
    [InspectorField]
    public float Shield { get; set; } = 100f;

    // implicit property backing field will be serialized by Unity
    [InspectorField(Label="Shield Regeneration")]
    public float ShieldRegenSpeed { get; set; } = 10f;

    // no backing field, will not be serialized by Unity
    // since marked as "read-only", it will not be editable from the inspector
    [InspectorField(Label="Health + Shield", ReadOnly)]
    public float HealthPlusShield
    {
        get => TotalHealth + Shield;
        set
        {
            float damage = HealthPlusShield - value;
            float absorbed = Mathf.Min(Shield, damage);

            Shield -= absorbed;
            TotalHealth -= (damage - absorbed);
        }
    }

    void Update()
    {
        // check if damaged
        if (HasReceivedDamage())
        {
            HealthPlusShield -= 1f;
        }

        // slowly regenerate shield
        if (Shield < 100f)
        {
            Shield += ShieldRegenSpeed * Time.deltaTime;
        }
    }
}
```

### Interface Fields

By default, Unity does not support fields of interface types.

The `[RequiresType(type)]` attribute adds support to interface field types.

```csharp
using UnityAdvancedInspector;
using UnityEngine;

public interface IWeapon
{
    string Name { get; }
    float Power { get; }
    int Capacity { get; }

    void FireBullet(Transform origin);
}

public class MachineGun : MonoBehaviour, IWeapon
{
    public string Name { get; set; }
    public float Power { get; set; }
    public int Capacity { get; set; }

    public void FireBullet(Transform origin)
    {
        // ...
    }
}

public class PlayerCharacter : MonoBehaviour
{
    [SerializeField, RequiresType(typeof(IWeapon))]
    Object _weapon;

    public IWeapon Weapon
    {
        get => _weapon;
        set => _weapon = (IWeapon)value;
    }

    void Update()
    {
        // ...

        Weapon.FireBullet(transform);
    }
}
```

### Scene Fields

Unity comes with an undocumented feature: a scene selector for fields that
should contain scene names.
This scene selector can be activated with the `[Scene]` attribute.

```csharp
using UnityAdvancedInspector;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [Scene]
    public string SceneName { get; set; }

    void Update()
    {
        // ...

        SceneManager.LoadScene(SceneName)
    }
}
```


## License

This project is licensed under the [MIT license](LICENSE).
