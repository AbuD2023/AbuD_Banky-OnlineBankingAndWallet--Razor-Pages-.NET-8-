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

/// شاشة التحويل المالي لمشترك برقم الهاتف (Transfer By Phone Screen)
/// تتيح للعميل اختيار محفظة المصدر، فحص المستلم، حساب الرسوم والعمولة المقررة من الإدارة، وتأكيد التحويل
class TransferByPhoneScreen extends StatefulWidget {
  const TransferByPhoneScreen({super.key});

  @override
  State<TransferByPhoneScreen> createState() => _TransferByPhoneScreenState();
}

class _TransferByPhoneScreenState extends State<TransferByPhoneScreen> {
  final _formKey = GlobalKey<FormState>();
  final _phoneController = TextEditingController();
  final _amountController = TextEditingController();
  final _noteController = TextEditingController();

  WalletModel? _selectedWallet;
  RecipientLookupResult? _recipientResult;

  bool _isCheckingRecipient = false;
  bool _isTransferring = false;
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
    _phoneController.dispose();
    _amountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// التحقق من رقم هاتف المستلم
  Future<void> _checkRecipient() async {
    final phone = _phoneController.text.trim();
    if (phone.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى إدخال رقم هاتف المستلم')),
      );
      return;
    }

    setState(() {
      _isCheckingRecipient = true;
      _lookupError = null;
      _recipientResult = null;
    });

    final currency = _selectedWallet?.currencyCode ?? 'YER';
    final result = await ApiService.lookupRecipient(phone: phone, currencyCode: currency);

    if (!mounted) return;
    setState(() => _isCheckingRecipient = false);

    if (result != null) {
      setState(() {
        _recipientResult = result;
        _feeDescription = result.feeDescription;
      });
      _calculateFee();
    } else {
      setState(() => _lookupError = 'رقم الهاتف غير مسجل في النظام أو الحساب غير موثق');
    }
  }

  /// احتساب الرسوم والعمولة المحددة من قبل مدير النظام لحظياً
  Future<void> _calculateFee() async {
    final amountText = _amountController.text.trim();
    final amount = double.tryParse(amountText);
    if (amount == null || amount <= 0) {
      setState(() => _calculatedFee = 0.00);
      return;
    }

    setState(() => _isCalculatingFee = true);

    final preview = await ApiService.calculateFeePreview(
      operationType: 'TransferByPhone',
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

  /// تنفيذ التحويل المالي وتأكيد العملية
  Future<void> _handleTransfer() async {
    if (!_formKey.currentState!.validate()) return;

    if (_recipientResult == null) {
      await _checkRecipient();
      if (!mounted) return;
      if (_recipientResult == null) return;
    }

    final amount = double.parse(_amountController.text.trim());
    final totalRequired = amount + _calculatedFee;

    if (_selectedWallet == null || _selectedWallet!.balance < totalRequired) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('رصيد محفظتك غير كافٍ لتغطية المبلغ والرسوم (${totalRequired.toStringAsFixed(2)} ${_selectedWallet?.currencySymbol})'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() => _isTransferring = true);

    final result = await ApiService.transferByPhone(
      receiverPhone: _phoneController.text.trim(),
      currencyCode: _selectedWallet!.currencyCode,
      amount: amount,
      note: _noteController.text.trim().isNotEmpty ? _noteController.text.trim() : null,
    );

    if (!mounted) return;
    setState(() => _isTransferring = false);

    if (result['success'] == true) {
      final auth = Provider.of<AuthProvider>(context, listen: false);
      await auth.refreshWallets();
      if (!mounted) return;

      SuccessReceiptDialog.show(
        context: context,
        title: 'تم التحويل بنجاح!',
        message: 'تم إرسال المبلغ بنجاح إلى ${_recipientResult?.displayName}',
        amount: amount,
        fee: _calculatedFee,
        currencySymbol: _selectedWallet!.currencySymbol,
        recipientName: _recipientResult?.displayName,
        onDone: () => Navigator.pop(context),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشل التحويل'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final auth = Provider.of<AuthProvider>(context);
    final amount = double.tryParse(_amountController.text.trim()) ?? 0.0;

    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويل لمشترك برقم الهاتف'),
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
                // 1. اختيار المحفظة والعملة المصدر
                CurrencyDropdown(
                  label: 'اختر المحفظة والعملة للتحويل منها',
                  wallets: auth.wallets,
                  selectedWallet: _selectedWallet,
                  onChanged: (val) {
                    setState(() {
                      _selectedWallet = val;
                      _recipientResult = null;
                    });
                    _calculateFee();
                  },
                ),
                const SizedBox(height: 18),

                // 2. إدخال رقم هاتف المستلم والتحقق
                const Text('رقم هاتف المستلم', style: TextStyle(fontSize: 13, fontWeight: FontWeight.bold)),
                const SizedBox(height: 8),
                Row(
                  children: [
                    Expanded(
                      child: TextFormField(
                        controller: _phoneController,
                        keyboardType: TextInputType.phone,
                        decoration: const InputDecoration(
                          hintText: '77XXXXXXX',
                          prefixIcon: Icon(Icons.phone_outlined),
                        ),
                        onChanged: (_) {
                          if (_recipientResult != null) {
                            setState(() => _recipientResult = null);
                          }
                        },
                      ),
                    ),
                    const SizedBox(width: 10),
                    PrimaryButton(
                      label: 'تحقق',
                      icon: Icons.search_rounded,
                      isLoading: _isCheckingRecipient,
                      onPressed: _checkRecipient,
                    ),
                  ],
                ),
                const SizedBox(height: 12),

                // بطاقة نتيجة فحص المستلم
                if (_recipientResult != null)
                  RecipientInfoCard(
                    title: _recipientResult!.displayName,
                    phone: _recipientResult!.phone,
                    isNameMasked: _recipientResult!.isNameMasked,
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

                // 3. إدخال مبلغ التحويل
                AmountInputField(
                  controller: _amountController,
                  currencyCode: _selectedWallet?.currencyCode,
                  currencySymbol: _selectedWallet?.currencySymbol,
                  onChanged: (_) => _calculateFee(),
                ),
                const SizedBox(height: 16),

                // 4. ملخص الرسوم والعمولة المحسوبة
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

                // 5. بيان وملاحظات التحويل
                TextFormField(
                  controller: _noteController,
                  decoration: const InputDecoration(
                    labelText: 'البيان أو الملاحظة (اختياري)',
                    hintText: 'سداد قيمة خدمات، إيجار، إلخ',
                    prefixIcon: Icon(Icons.note_alt_outlined),
                  ),
                ),
                const SizedBox(height: 32),

                // 6. زر تأكيد التحويل
                PrimaryButton(
                  label: 'تأكيد وتنفيذ التحويل المالي',
                  icon: Icons.send_rounded,
                  isLoading: _isTransferring,
                  onPressed: _handleTransfer,
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
