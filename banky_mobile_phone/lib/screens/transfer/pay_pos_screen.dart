import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/pos_model.dart';
import '../../models/wallet_model.dart';
import '../../services/api_service.dart';
import '../../widgets/amount_input_field.dart';
import '../../widgets/currency_dropdown.dart';
import '../../widgets/custom_card.dart';
import '../../widgets/fee_summary_card.dart';
import '../../widgets/primary_button.dart';
import '../../widgets/recipient_info_card.dart';
import '../../widgets/success_receipt_dialog.dart';

/// شاشة الدفع والشراء عبر نقطة بيع (Pay POS Screen)
/// تطبق ميزة حماية الخصوصية بالرقم البديل مع احتساب رسوم المشتريات إن وجدت
class PayPosScreen extends StatefulWidget {
  const PayPosScreen({super.key});

  @override
  State<PayPosScreen> createState() => _PayPosScreenState();
}

class _PayPosScreenState extends State<PayPosScreen> {
  final _formKey = GlobalKey<FormState>();
  final _posCodeController = TextEditingController(text: 'POS-100200');
  final _amountController = TextEditingController();
  final _noteController = TextEditingController();

  WalletModel? _selectedWallet;
  PosLookupResult? _posResult;

  bool _isCheckingPos = false;
  bool _isPaying = false;
  bool _isCalculatingFee = false;
  String? _lookupError;

  double _calculatedFee = 0.00;
  String? _feeDescription;

  @override
  void initState() {
    super.initState();
    final auth = Provider.of<AuthProvider>(context, listen: false);
    if (auth.wallets.isNotEmpty) {
      _selectedWallet = auth.wallets.first;
    }
  }

  @override
  void dispose() {
    _posCodeController.dispose();
    _amountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// التحقق من كود نقطة البيع
  Future<void> _checkPos() async {
    final code = _posCodeController.text.trim().toUpperCase();
    if (code.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال كود نقطة البيع')),
      );
      return;
    }

    setState(() {
      _isCheckingPos = true;
      _lookupError = null;
      _posResult = null;
    });

    final result = await ApiService.lookupPos(code);

    if (!mounted) return;
    setState(() => _isCheckingPos = false);

    if (result != null) {
      setState(() => _posResult = result);
      _calculateFee();
    } else {
      setState(() => _lookupError = 'كود نقطة البيع غير صحيح أو غير مفعل');
    }
  }

  /// احتساب رسوم خدمة الدفع
  Future<void> _calculateFee() async {
    final amountText = _amountController.text.trim();
    final amount = double.tryParse(amountText);
    if (amount == null || amount <= 0) {
      setState(() => _calculatedFee = 0.00);
      return;
    }

    setState(() => _isCalculatingFee = true);

    final preview = await ApiService.calculateFeePreview(
      operationType: 'PosPayment',
      amount: amount,
      currency: _selectedWallet?.currencyCode ?? 'YER',
    );

    if (!mounted) return;
    setState(() {
      _isCalculatingFee = false;
      if (preview != null) {
        _calculatedFee = (preview['feeAmount'] as num?)?.toDouble() ?? 0.00;
        _feeDescription = preview['feeRuleDescription']?.toString();
      }
    });
  }

  /// تنفيذ الدفع لنقطة البيع
  Future<void> _handlePayment() async {
    if (!_formKey.currentState!.validate()) return;

    if (_posResult == null) {
      await _checkPos();
      if (!mounted) return;
      if (_posResult == null) return;
    }

    final amount = double.parse(_amountController.text.trim());
    final totalRequired = amount + _calculatedFee;

    if (_selectedWallet == null || _selectedWallet!.balance < totalRequired) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('رصيد المحفظة غير كافٍ لتغطية المبلغ والرسوم (${totalRequired.toStringAsFixed(2)} ${_selectedWallet?.currencySymbol})'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() => _isPaying = true);

    final result = await ApiService.payToPos(
      posCode: _posCodeController.text.trim().toUpperCase(),
      currencyCode: _selectedWallet!.currencyCode,
      amount: amount,
      note: _noteController.text.trim().isNotEmpty ? _noteController.text.trim() : null,
    );

    if (!mounted) return;
    setState(() => _isPaying = false);

    if (result['success'] == true) {
      final auth = Provider.of<AuthProvider>(context, listen: false);
      await auth.refreshWallets();
      if (!mounted) return;

      SuccessReceiptDialog.show(
        context: context,
        title: 'تم الدفع بنجاح!',
        message: 'تم سداد مبلغ ${amount.toStringAsFixed(2)} ${_selectedWallet!.currencySymbol} لـ (${_posResult?.name})',
        amount: amount,
        fee: _calculatedFee,
        currencySymbol: _selectedWallet!.currencySymbol,
        recipientName: _posResult?.name,
        onDone: () => Navigator.pop(context),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشل الدفع'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    final user = auth.user;
    final amount = double.tryParse(_amountController.text.trim()) ?? 0.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text('دفع وشراء عبر نقطة بيع (POS)'),
        centerTitle: true,
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(20),
          child: Form(
            key: _formKey,
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.stretch,
              children: [
                // إشعار الخصوصية
                if (user?.hidePhoneOnPos == true) ...[
                  CustomCard(
                    color: Colors.blue.withValues(alpha: 0.08),
                    border: Border.all(color: Colors.blue.withValues(alpha: 0.25)),
                    child: Row(
                      children: [
                        const Icon(Icons.shield_rounded, color: Colors.blue, size: 24),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            'ميزة الخصوصية مفعلة: سيظهر للتاجر رقمك البديل (${user?.posAliasPhone ?? "ALIAS-000000"}) بدلاً من رقم هاتفك الحقيقي.',
                            style: const TextStyle(fontSize: 12, color: Colors.blue),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),
                ],

                // 1. اختيار محفظة الدفع
                CurrencyDropdown(
                  label: 'اختر المحفظة للدفع منها',
                  wallets: auth.wallets,
                  selectedWallet: _selectedWallet,
                  onChanged: (val) {
                    setState(() => _selectedWallet = val);
                    _calculateFee();
                  },
                ),
                const SizedBox(height: 18),

                // 2. كود نقطة البيع
                const Text('كود نقطة البيع (أو الـ QR)', style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _posCodeController,
                        textCapitalization: TextCapitalization.characters,
                        decoration: const InputDecoration(
                          hintText: 'POS-XXXXXX',
                          prefixIcon: Icon(Icons.store_outlined),
                        ),
                        onChanged: (_) {
                          if (_posResult != null) setState(() => _posResult = null);
                        },
                      ),
                    ),
                    const SizedBox(width: 10),
                    PrimaryButton(
                      label: 'تحقق',
                      icon: Icons.qr_code_scanner_rounded,
                      color: Colors.purple,
                      isLoading: _isCheckingPos,
                      onPressed: _checkPos,
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // بطاقة نقطة البيع
                if (_posResult != null)
                  RecipientInfoCard(
                    title: _posResult!.name,
                    subtitle: 'التاجر: ${_posResult!.merchantDisplayName} • ${_posResult!.category ?? "متجر"}',
                    address: _posResult!.address,
                    isPos: true,
                  )
                else if (_lookupError != null)
                  CustomCard(
                    color: Colors.red.withValues(alpha: 0.08),
                    border: Border.all(color: Colors.red.withValues(alpha: 0.3)),
                    child: Row(
                      children: [
                        const Icon(Icons.error_outline, color: Colors.red),
                        const SizedBox(width: 8),
                        Expanded(child: Text(_lookupError!, style: const TextStyle(color: Colors.red, fontSize: 13))),
                      ],
                    ),
                  ),
                const SizedBox(height: 18),

                // 3. مبلغ المشتريات
                AmountInputField(
                  controller: _amountController,
                  currencyCode: _selectedWallet?.currencyCode,
                  currencySymbol: _selectedWallet?.currencySymbol,
                  onChanged: (_) => _calculateFee(),
                ),
                const SizedBox(height: 16),

                // 4. ملخص الرسوم
                if (amount > 0) ...[
                  FeeSummaryCard(
                    amount: amount,
                    fee: _calculatedFee,
                    currencySymbol: _selectedWallet?.currencySymbol ?? '',
                    feeDescription: _feeDescription,
                    isLoading: _isCalculatingFee,
                  ),
                  const SizedBox(height: 16),
                ],

                // 5. رقم الفاتورة أو ملاحظات
                TextFormField(
                  controller: _noteController,
                  decoration: const InputDecoration(
                    labelText: 'رقم الفاتورة أو البيان (اختياري)',
                    hintText: 'فاتورة مشتريات #1024',
                    prefixIcon: Icon(Icons.receipt_outlined),
                  ),
                ),
                const SizedBox(height: 32),

                // 6. زر الدفع
                PrimaryButton(
                  label: 'تأكيد الدفع لنقطة البيع',
                  icon: Icons.check_circle_outline_rounded,
                  color: Colors.purple.shade700,
                  isLoading: _isPaying,
                  onPressed: _handlePayment,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
