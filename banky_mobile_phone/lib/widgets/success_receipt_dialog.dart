import 'package:flutter/material.dart';
import 'primary_button.dart';

/// نافذة إيصال نجاح العملية المصرفية (Success Receipt Dialog Widget)
/// يعرض إشعاراً جمالياً بنجاح العملية مع رقم العملية والمبلغ وتفاصيل الرسوم وزر الإغلاق
class SuccessReceiptDialog extends StatelessWidget {
  final String title;
  final String message;
  final String? transactionNumber;
  final double? amount;
  final double? fee;
  final String? currencySymbol;
  final String? recipientName;
  final VoidCallback onDone;

  const SuccessReceiptDialog({
    super.key,
    required this.title,
    required this.message,
    this.transactionNumber,
    this.amount,
    this.fee,
    this.currencySymbol,
    this.recipientName,
    required this.onDone,
  });

  static Future<void> show({
    required BuildContext context,
    required String title,
    required String message,
    String? transactionNumber,
    double? amount,
    double? fee,
    String? currencySymbol,
    String? recipientName,
    required VoidCallback onDone,
  }) {
    return showDialog(
      context: context,
      barrierDismissible: false,
      builder: (_) => SuccessReceiptDialog(
        title: title,
        message: message,
        transactionNumber: transactionNumber,
        amount: amount,
        fee: fee,
        currencySymbol: currencySymbol,
        recipientName: recipientName,
        onDone: onDone,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return AlertDialog(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      contentPadding: const EdgeInsets.all(24),
      content: Column(
        mainAxisSize: MainAxisSize.min,
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              color: Colors.green.withValues(alpha: 0.12),
              shape: BoxShape.circle,
            ),
            child: const Icon(Icons.check_circle_rounded, color: Colors.green, size: 54),
          ),
          const SizedBox(height: 16),
          Text(
            title,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 8),
          Text(
            message,
            textAlign: TextAlign.center,
            style: TextStyle(fontSize: 13, color: theme.colorScheme.onSurface.withValues(alpha: 0.7)),
          ),
          if (amount != null && currencySymbol != null) ...[
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.4),
                borderRadius: BorderRadius.circular(14),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text('المبلغ المنفذ:', style: TextStyle(fontSize: 12)),
                      Text('${amount!.toStringAsFixed(2)} $currencySymbol', style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13)),
                    ],
                  ),
                  if (fee != null && fee! > 0) ...[
                    const SizedBox(height: 4),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('الرسوم:', style: TextStyle(fontSize: 12)),
                        Text('${fee!.toStringAsFixed(2)} $currencySymbol', style: const TextStyle(color: Colors.red, fontWeight: FontWeight.bold, fontSize: 12)),
                      ],
                    ),
                  ],
                  if (recipientName != null) ...[
                    const Divider(height: 12),
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        const Text('الطرف الآخر:', style: TextStyle(fontSize: 12)),
                        Text(recipientName!, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 12)),
                      ],
                    ),
                  ],
                ],
              ),
            ),
          ],
          const SizedBox(height: 24),
          PrimaryButton(
            label: 'تم والعودة للرئيسية',
            icon: Icons.done_all_rounded,
            onPressed: () {
              Navigator.pop(context);
              onDone();
            },
          ),
        ],
      ),
    );
  }
}
