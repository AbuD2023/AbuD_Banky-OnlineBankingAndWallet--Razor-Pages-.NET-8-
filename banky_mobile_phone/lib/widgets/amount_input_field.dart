import 'package:flutter/material.dart';

/// ويدجت حقل إدخال المبالغ المالية (Amount Input Field Widget)
/// يوفر حقل إدخال مخصص للأرقام والمبالغ مع لاحقة العملة والتحقق من صحة المبلغ
class AmountInputField extends StatelessWidget {
  final TextEditingController controller;
  final String? label;
  final String? currencyCode;
  final String? currencySymbol;
  final ValueChanged<String>? onChanged;
  final String? Function(String?)? validator;
  final bool autofocus;

  const AmountInputField({
    super.key,
    required this.controller,
    this.label,
    this.currencyCode,
    this.currencySymbol,
    this.onChanged,
    this.validator,
    this.autofocus = false,
  });

  @override
  Widget build(BuildContext context) {
    final effectiveLabel = label ?? 'المبلغ المطلوب ${currencySymbol != null ? "($currencySymbol)" : ""}';

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(
          effectiveLabel,
          style: const TextStyle(
            fontSize: 13,
            fontWeight: FontWeight.bold,
          ),
        ),
        const SizedBox(height: 8),
        TextFormField(
          controller: controller,
          keyboardType: const TextInputType.numberWithOptions(decimal: true),
          autofocus: autofocus,
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          decoration: InputDecoration(
            hintText: '0.00',
            prefixIcon: const Icon(Icons.attach_money_rounded),
            suffixText: currencyCode ?? currencySymbol,
            suffixStyle: const TextStyle(fontWeight: FontWeight.bold),
            contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
          ),
          onChanged: onChanged,
          validator: validator ??
              (val) {
                if (val == null || val.trim().isEmpty) return 'يرجى إدخال المبلغ';
                final n = double.tryParse(val.trim());
                if (n == null || n <= 0) return 'المبلغ يجب أن يكون أكبر من الصفر';
                return null;
              },
        ),
      ],
    );
  }
}
