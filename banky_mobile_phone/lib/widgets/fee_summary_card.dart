import 'package:flutter/material.dart';
import 'custom_card.dart';

/// ويدجت بطاقة ملخص الرسوم والحساب الإجمالي (Fee Summary Card Widget)
/// يعرض للعميل تفصيلاً شفافاً للمبلغ الأصلي، الرسوم والعمولة المقتطعة، والمبلغ الإجمالي المخصوم
class FeeSummaryCard extends StatelessWidget {
  final double amount;
  final double fee;
  final String currencySymbol;
  final String? feeDescription;
  final double? targetAmount;
  final String? targetCurrencySymbol;
  final double? exchangeRate;
  final bool isLoading;

  const FeeSummaryCard({
    super.key,
    required this.amount,
    required this.fee,
    required this.currencySymbol,
    this.feeDescription,
    this.targetAmount,
    this.targetCurrencySymbol,
    this.exchangeRate,
    this.isLoading = false,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final total = amount + fee;

    if (isLoading) {
      return CustomCard(
        color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.3),
        child: const Center(
          child: Padding(
            padding: EdgeInsets.all(12),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                SizedBox(
                  width: 18,
                  height: 18,
                  child: CircularProgressIndicator(strokeWidth: 2),
                ),
                SizedBox(width: 12),
                Text('جاري احتساب الرسوم وأسعار الصرف...', style: TextStyle(fontSize: 12, fontWeight: FontWeight.bold)),
              ],
            ),
          ),
        ),
      );
    }

    if (amount <= 0) return const SizedBox.shrink();

    return CustomCard(
      color: theme.colorScheme.primaryContainer.withValues(alpha: 0.15),
      border: Border.all(color: theme.colorScheme.primary.withValues(alpha: 0.25)),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Row(
            children: [
              Icon(Icons.receipt_long_rounded, color: theme.primaryColor, size: 20),
              const SizedBox(width: 8),
              const Text(
                'ملخص العملية والرسوم',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
              ),
              const Spacer(),
              if (fee == 0)
                Container(
                  padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 2),
                  decoration: BoxDecoration(
                    color: Colors.green.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(6),
                  ),
                  child: const Text('معفاة من الرسوم', style: TextStyle(color: Colors.green, fontSize: 11, fontWeight: FontWeight.bold)),
                )
              else if (feeDescription != null)
                Text(
                  feeDescription!,
                  style: TextStyle(fontSize: 11, color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
                ),
            ],
          ),
          const Divider(height: 16),
          
          // المبلغ المطلوب
          _buildRow('المبلغ الأساسي:', '${amount.toStringAsFixed(2)} $currencySymbol'),
          const SizedBox(height: 6),

          // الرسوم
          _buildRow(
            'رسوم / عمولة البنك:',
            fee > 0 ? '${fee.toStringAsFixed(2)} $currencySymbol' : '0.00 $currencySymbol (مجاناً)',
            valueColor: fee > 0 ? Colors.red : Colors.green,
          ),
          const SizedBox(height: 6),

          // الإجمالي المخصوم
          _buildRow(
            'الإجمالي المخصوم من محفظتك:',
            '${total.toStringAsFixed(2)} $currencySymbol',
            isBold: true,
            valueColor: theme.colorScheme.primary,
          ),

          // في حال المصارفة والتحويل بين العملات
          if (targetAmount != null && targetCurrencySymbol != null) ...[
            const Divider(height: 16),
            _buildRow(
              'المبلغ المستلم في محفظة ($targetCurrencySymbol):',
              '${targetAmount!.toStringAsFixed(2)} $targetCurrencySymbol',
              isBold: true,
              valueColor: Colors.green,
            ),
            if (exchangeRate != null) ...[
              const SizedBox(height: 4),
              _buildRow(
                'سعر الصرف المعتمد:',
                '1 $currencySymbol = ${exchangeRate!.toStringAsFixed(4)} $targetCurrencySymbol',
                isSmall: true,
              ),
            ],
          ],
        ],
      ),
    );
  }

  Widget _buildRow(String title, String value, {bool isBold = false, Color? valueColor, bool isSmall = false}) {
    return Row(
      mainAxisAlignment: MainAxisAlignment.spaceBetween,
      children: [
        Text(
          title,
          style: TextStyle(
            fontSize: isSmall ? 11 : 12,
            fontWeight: isBold ? FontWeight.bold : FontWeight.w500,
            color: isSmall ? Colors.grey : null,
          ),
        ),
        Text(
          value,
          style: TextStyle(
            fontSize: isSmall ? 11 : (isBold ? 14 : 12),
            fontWeight: isBold ? FontWeight.bold : FontWeight.w600,
            color: valueColor,
          ),
        ),
      ],
    );
  }
}
