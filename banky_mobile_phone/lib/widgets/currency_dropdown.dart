import 'package:flutter/material.dart';
import '../models/wallet_model.dart';
import 'custom_card.dart';

/// ويدجت منسدلة اختيار المحفظة والعملة (Currency Dropdown Widget)
/// يعرض للمستخدم قائمة المحافظ المالية المتاحة مع الرصيد الحالي والاسم والرمز بصورة جمالية
class CurrencyDropdown extends StatelessWidget {
  final String label;
  final List<WalletModel> wallets;
  final WalletModel? selectedWallet;
  final ValueChanged<WalletModel?> onChanged;
  final String? helperText;

  const CurrencyDropdown({
    super.key,
    this.label = 'اختر المحفظة والعملة',
    required this.wallets,
    required this.selectedWallet,
    required this.onChanged,
    this.helperText,
  });

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        if (label.isNotEmpty) ...[
          Text(
            label,
            style: const TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.bold,
            ),
          ),
          const SizedBox(height: 8),
        ],
        CustomCard(
          padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
          child: DropdownButtonHideUnderline(
            child: DropdownButton<WalletModel>(
              value: selectedWallet != null && wallets.any((w) => w.id == selectedWallet!.id)
                  ? wallets.firstWhere((w) => w.id == selectedWallet!.id)
                  : (wallets.isNotEmpty ? wallets.first : null),
              isExpanded: true,
              borderRadius: BorderRadius.circular(16),
              icon: const Icon(Icons.keyboard_arrow_down_rounded),
              items: wallets.map((wallet) {
                return DropdownMenuItem<WalletModel>(
                  value: wallet,
                  child: Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Row(
                        children: [
                          Container(
                            padding: const EdgeInsets.all(6),
                            decoration: BoxDecoration(
                              color: theme.primaryColor.withValues(alpha: 0.1),
                              shape: BoxShape.circle,
                            ),
                            child: Icon(Icons.account_balance_wallet_outlined, size: 18, color: theme.primaryColor),
                          ),
                          const SizedBox(width: 10),
                          Text(
                            '${wallet.currencyNameAr} (${wallet.currencyCode})',
                            style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 13),
                          ),
                        ],
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: Colors.green.withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(8),
                        ),
                        child: Text(
                          '${wallet.balance.toStringAsFixed(2)} ${wallet.currencySymbol}',
                          style: const TextStyle(
                            color: Colors.green,
                            fontWeight: FontWeight.bold,
                            fontSize: 12,
                          ),
                        ),
                      ),
                    ],
                  ),
                );
              }).toList(),
              onChanged: onChanged,
            ),
          ),
        ),
        if (helperText != null) ...[
          const SizedBox(height: 4),
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 4),
            child: Text(
              helperText!,
              style: TextStyle(fontSize: 11, color: theme.colorScheme.onSurface.withValues(alpha: 0.6)),
            ),
          ),
        ],
      ],
    );
  }
}
