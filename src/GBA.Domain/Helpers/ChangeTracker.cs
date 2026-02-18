using System;
using System.Reflection;
using GBA.Domain.AuditEntities;

namespace GBA.Domain.Helpers;

public static class ChangeTracker {
    public static AuditEntity GetAuditInfo(object oldEntity, object newEntity) {
        AuditEntity auditEntity = new();

        if (oldEntity != null) {
            auditEntity.Type = AuditEventType.Edit;

            PropertyInfo[] oldProperties = oldEntity.GetType().GetProperties();

            foreach (PropertyInfo property in oldProperties) {
                if (property.Name.Equals("Id")) continue;

                if (property.Name.Equals("NetUid")) continue;

                if (property.Name.Equals("Deleted")) continue;

                if (property.Name.Equals("Created")) continue;

                if (property.Name.Equals("Updated")) continue;

                if (property.GetGetMethod().IsVirtual) continue;

                if (property.Name.EndsWith("Id")) continue;

                if (property.PropertyType == typeof(decimal)) {
                    property.SetValue(
                        oldEntity,
                        decimal.Round(
                            Convert.ToDecimal(Convert.ToDecimal(property.GetValue(oldEntity)).ToString("G29")),
                            4,
                            MidpointRounding.AwayFromZero
                        )
                    );

                    property.SetValue(
                        newEntity,
                        decimal.Round(
                            Convert.ToDecimal(Convert.ToDecimal(property.GetValue(newEntity)).ToString("G29")),
                            4,
                            MidpointRounding.AwayFromZero
                        )
                    );
                }

                string oldValue = Convert.ToString(property.GetValue(oldEntity));
                string newValue = Convert.ToString(newEntity.GetType().GetProperty(property.Name).GetValue(newEntity));

                if (string.IsNullOrEmpty(oldValue) && string.IsNullOrEmpty(newValue)) continue;

                if (!oldValue.Equals(newValue)) {
                    auditEntity.OldValues.Add(new AuditEntityProperty {
                        Name = property.Name,
                        Value = oldValue,
                        Type = AuditEntityPropertyType.Old
                    });

                    auditEntity.NewValues.Add(new AuditEntityProperty {
                        Name = property.Name,
                        Value = newValue,
                        Type = AuditEntityPropertyType.New
                    });
                }
            }
        } else {
            auditEntity.Type = AuditEventType.New;

            PropertyInfo[] newProperties = newEntity.GetType().GetProperties();

            foreach (PropertyInfo property in newProperties) {
                if (property.Name.Equals("Id")) continue;

                if (property.Name.Equals("NetUid")) continue;

                if (property.Name.Equals("Created")) continue;

                if (property.Name.Equals("Updated")) continue;

                if (property.GetGetMethod().IsVirtual) continue;

                if (property.Name.EndsWith("Id")) continue;

                string newValue = Convert.ToString(newEntity.GetType().GetProperty(property.Name).GetValue(newEntity));

                if (string.IsNullOrEmpty(newValue)) continue;

                auditEntity.NewValues.Add(new AuditEntityProperty {
                    Name = property.Name,
                    Value = newValue,
                    Type = AuditEntityPropertyType.New
                });
            }
        }

        return auditEntity;
    }
}