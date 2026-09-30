import 'package:flutter/material.dart';
import 'custom_card.dart';

/// ويدجت بطاقة عرض بيانات المستلم ونقاط البيع (Recipient Info Card Widget)
/// يعرض للمرسل اسم المستلم، التحقق، شارات الخصوصية (الاسم المقنع بالحروف الأولى)، ورقم الهاتف أو نقطة البيع
class RecipientInfoCard extends StatelessWidget {
  final String title;
  final String? subtitle;
  final String? phone;
  final bool isNameMasked;
  final bool isPos;
  final String? category;
  final String? address;

  const RecipientInfoCard({
    super.key,
    required this.title,
    this.subtitle,
    this.phone,
    this.isNameMasked = false,
    this.isPos = false,
    this.category,
    this.address,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final iconColor = isPos ? Colors.purple : Colors.green;
    final iconBg = iconColor.withValues(alpha: 0.1);

    return CustomCard(
      color: iconBg,
      border: Border.all(color: iconColor.withValues(alpha: 0.3)),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.all(10),
            decoration: BoxDecoration(
              color: Colors.white,
              shape: BoxShape.circle,
              boxShadow: [
                BoxShadow(
                  color: iconColor.withValues(alpha: 0.15),
                  blurRadius: 8,
                ),
              ],
            ),
            child: Icon(
              isPos ? Icons.storefront_rounded : Icons.person_rounded,
              color: iconColor,
              size: 28,
            ),
          ),
          const SizedBox(width: 14),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        title,
                        style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                        overflow: TextOverflow.ellipsis,
                      ),
                    ),
                    if (isNameMasked)
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                        decoration: BoxDecoration(
                          color: Colors.blue.withValues(alpha: 0.15),
                          borderRadius: BorderRadius.circular(6),
                        ),
                        child: const Text('اسم مقنع بالرموز', style: TextStyle(fontSize: 10, color: Colors.blue, fontWeight: FontWeight.bold)),
                      ),
                  ],
                ),
                if (phone != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    'رقم الهاتف: $phone',
                    style: TextStyle(fontSize: 12, color: theme.colorScheme.onSurface.withValues(alpha: 0.65)),
                  ),
                ],
                if (subtitle != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    subtitle!,
                    style: TextStyle(fontSize: 12, color: theme.colorScheme.onSurface.withValues(alpha: 0.65)),
                  ),
                ],
                if (address != null) ...[
                  const SizedBox(height: 2),
                  Text(
                    'العنوان: $address',
                    style: TextStyle(fontSize: 11, color: theme.colorScheme.onSurface.withValues(alpha: 0.5)),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }
}
