using BepInEx;
using RoR2;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RoR2.Run;

namespace DamageIndicatorFix
{
  [BepInPlugin("com.Nuxlar.DamageIndicatorFix", "DamageIndicatorFix", "1.1.0")]

  public class DamageIndicatorFix : BaseUnityPlugin
  {

    public void Awake()
    {
      On.RoR2.PostProcessing.DamageIndicator.Awake += (orig, self) =>
      {
        orig(self);
        self.indicatorDuration = 0.5f;
      };
      On.RoR2.PostProcessing.DamageIndicator.OnClientDamage += (orig, self, damageDealtMessage) =>
      {
        if (damageDealtMessage.victim == null || !(bool)self.cameraRigController || !(damageDealtMessage.victim == self.cameraRigController.localUserViewer.cachedBodyObject))
          return;
        if ((bool)self.cameraRigController)
        {
          if ((bool)self.cameraRigController.target)
          {
            float num = 0f;
            float healthPercentage = 1f;
            HealthComponent component = damageDealtMessage.victim.GetComponent<HealthComponent>();
            if ((bool)component)
            {
              float damagePercentage = Mathf.Clamp((component.health + damageDealtMessage.damage) / component.fullHealth, 0f, 1f);
              healthPercentage = Mathf.Clamp(component.health / component.fullHealth, 0f, 1f);

              num = Mathf.Clamp(damagePercentage - healthPercentage, 0.3f, 0.9f);
              self._indicatorValue = num;
              for (int i = 0; i < self.indicators.Length; i++)
              {
                self._indicatorTimestamps[i].hitTimeStamp = FixedTimeStamp.now;
                self._indicatorTimestamps[i].isActive = true;
              }
            }
          }
        }
      };
      On.RoR2.PostProcessing.DamageIndicator.OnRenderImage += (orig, self, source, destination) =>
      {
        if ((bool)self.cameraRigController)
        {
          if ((bool)self.cameraRigController.target)
          {
            float healthPercentage = 1f;
            HealthComponent component = self.cameraRigController.target.GetComponent<HealthComponent>();
            if ((bool)component)
            {
              healthPercentage = Mathf.Clamp(component.health / component.fullHealth, 0f, 1f);
            }

            if (healthPercentage <= 0.25f)
            {
              self._indicatorValue = 0.75f;
              for (int i = 0; i < self.indicators.Length; i++)
              {
                self._indicatorTimestamps[i].hitTimeStamp = FixedTimeStamp.now;
                self._indicatorTimestamps[i].isActive = true;
              }
            }
          }
        }
        if (Mathf.Approximately(self._indicatorValue, 0.0f))
        {
          if (self.isEnabled)
          {
            self.ResetIndicators();
            self.mat.SetVectorArray("_Indicators", self.indicators);
            self.isEnabled = false;
          }
          Graphics.Blit((Texture)source, destination, self.mat);
        }
        else
        {
          if (!self.isEnabled)
            self.isEnabled = true;
          self.mat.SetFloat("_BoxCornerRadius", self.boxCornerRadius);
          self.mat.SetFloat("_BoxFeather", self.boxFeather);
          self.mat.SetFloat("_BoxSize", self.GetBoxSize(self._indicatorValue));
          self.mat.SetColor("_TintColor", self.tintColor);
          self.mat.SetFloat("_IndicatorRadius", self.indicatorRadius);
          self.mat.SetFloat("_IndicatorFeather", self.indicatorFeather);
          for (int index = 0; index < self._indicatorTimestamps.Length; ++index)
          {
            if (self._indicatorTimestamps[index].isActive)
            {
              float timeSince = self._indicatorTimestamps[index].hitTimeStamp.timeSince;
              if ((double)timeSince >= (double)self.indicatorDuration)
                self._indicatorTimestamps[index].isActive = false;
              else
                self.indicators[index].z = Mathf.Clamp(self.indicatorOpacity.Evaluate(timeSince / self.indicatorDuration), 0.0f, 1f);
            }
          }
          self.mat.SetVectorArray("_Indicators", self.indicators);
          Graphics.Blit((Texture)source, destination, self.mat);
        }
      };
    }

  }
}