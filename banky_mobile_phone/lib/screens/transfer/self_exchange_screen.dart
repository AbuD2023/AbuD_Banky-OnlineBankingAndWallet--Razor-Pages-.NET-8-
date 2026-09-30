import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/wallet_model.dart';
import '../../services/api_service.dart';
import '../../widgets/amount_input_field.dart';
import '../../widgets/custom_card.dart';
import '../../widgets/fee_summary_card.dart';
import '../../widgets/primary_button.dart';
import '../../widgets/success_receipt_dialog.dart';

/// شاشة التحويل والمصارفة بين محافظ وحسابات العميل الخاصة (Self-Exchange Screen)
/// تتيح للعميل اختيار عملة المصدر والهدف، واحتساب سعر الصرف وعمولة المصارفة المحددة من الإدارة
class SelfExchangeScreen extends StatefulWidget {
  const SelfExchangeScreen({super.key});

  @override
  State<SelfExchangeScreen> createState() => _SelfExchangeScreenState();
}

class _SelfExchangeScreenState extends State<SelfExchangeScreen> {
  final _formKey = GlobalKey<FormState>();
  final _amountController = TextEditingController();
  final _noteController = TextEditingController();

  String _fromCurrency = 'YER';
  String _toCurrency = 'SAR';

  bool _isLoading = false;
  bool _isCalculating = false;

  double? _targetAmount;
  double? _exchangeRate;
  double _calculatedFee = 0.00;
  String? _feeDescription;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addPostFrameCallback((_) {
      final auth = Provider.of<AuthProvider>(context, listen: false);
      if (auth.wallets.isNotEmpty) {
        setState(() {
          _fromCurrency = auth.wallets.first.currencyCode;
          final otherWallets = auth.wallets.where((w) => w.currencyCode != _fromCurrency).toList();
          if (otherWallets.isNotEmpty) {
            _toCurrency = otherWallets.first.currencyCode;
          } else {
            _toCurrency = _fromCurrency == 'YER' ? 'SAR' : 'YER';
          }
        });
      }
    });
  }

  @override
  void dispose() {
    _amountController.dispose();
    _noteController.dispose();
    super.dispose();
  }

  /// احتساب سعر الصرف وعمولة المصارفة لحظياً
  Future<void> _calculateRate() async {
    final amountText = _amountController.text.trim();
    if (amountText.isEmpty) {
      setState(() {
        _targetAmount = null;
        _exchangeRate = null;
        _calculatedFee = 0.00;
      });
      return;
    }

    final amount = double.tryParse(amountText);
    if (amount == null || amount <= 0 || _fromCurrency == _toCurrency) {
      setState(() {
        _targetAmount = null;
        _exchangeRate = null;
        _calculatedFee = 0.00;
      });
      return;
    }

    setState(() => _isCalculating = true);

    final res = await ApiService.calculateExchange(
      fromCurrency: _fromCurrency,
      toCurrency: _toCurrency,
      amount: amount,
    );

    if (mounted) {
      setState(() {
        _isCalculating = false;
        if (res != null) {
          _targetAmount = (res['targetAmount'] as num?)?.toDouble();
          _exchangeRate = (res['exchangeRate'] as num?)?.toDouble();
          _calculatedFee = (res['fee'] as num?)?.toDouble() ?? 0.00;
          _feeDescription = res['feeDescription']?.toString();
        } else {
          _targetAmount = null;
          _exchangeRate = null;
          _calculatedFee = 0.00;
        }
      });
    }
  }

  /// تنفيذ عملية التحويل والمصارفة
  Future<void> _submitExchange() async {
    if (!_formKey.currentState!.validate()) return;

    if (_fromCurrency == _toCurrency) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى اختيار عملتين مختلفتين للتحويل والمصارفة')),
      );
      return;
    }

    final amount = double.parse(_amountController.text.trim());
    final totalRequired = amount + _calculatedFee;
    final auth = Provider.of<AuthProvider>(context, listen: false);

    final fromWallet = auth.wallets.firstWhere(
      (w) => w.currencyCode == _fromCurrency,
      orElse: () => WalletModel(id: '', accountNumber: '', currencyCode: _fromCurrency, currencyNameAr: '', currencyNameEn: '', currencySymbol: _fromCurrency, balance: 0, isActive: true),
    );

    if (fromWallet.balance < totalRequired) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('رصيدك غير كافٍ شامل العمولة (${totalRequired.toStringAsFixed(2)} $_fromCurrency). الرصيد المتاح: ${fromWallet.balance.toStringAsFixed(2)}'),
          backgroundColor: Colors.red,
        ),
      );
      return;
    }

    setState(() => _isLoading = true);

    final result = await ApiService.exchangeSelf(
      fromCurrencyCode: _fromCurrency,
      toCurrencyCode: _toCurrency,
      amount: amount,
      note: _noteController.text.trim().isNotEmpty ? _noteController.text.trim() : null,
    );

    if (!mounted) return;
    setState(() => _isLoading = false);

    if (result['success'] == true) {
      await auth.refreshWallets();
      if (!mounted) return;

      SuccessReceiptDialog.show(
        context: context,
        title: 'تمت المصارفة بنجاح!',
        message: 'تم تحويل $amount $_fromCurrency إلى ${_targetAmount?.toStringAsFixed(2)} $_toCurrency بنجاح.',
        amount: amount,
        fee: _calculatedFee,
        currencySymbol: _fromCurrency,
        onDone: () => Navigator.pop(context),
      );
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(result['message'] ?? 'فشلت العملية'),
          backgroundColor: Colors.red,
        ),
      );
    }
  }

  void _swapCurrencies() {
    setState(() {
      final temp = _fromCurrency;
      _fromCurrency = _toCurrency;
      _toCurrency = temp;
    });
    _calculateRate();
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);
    final availableCurrencies = ['YER', 'SAR', 'USD'];
    final amount = double.tryParse(_amountController.text.trim()) ?? 0.0;

    final fromWallet = auth.wallets.firstWhere(
      (w) => w.currencyCode == _fromCurrency,
      orElse: () => WalletModel(id: '', accountNumber: '', currencyCode: _fromCurrency, currencyNameAr: '', currencyNameEn: '', currencySymbol: _fromCurrency, balance: 0, isActive: true),
    );

    return Scaffold(
      appBar: AppBar(
        title: const Text('تحويل ومصارفة بين محافظي'),
        centerTitle: true,
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: _formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // بطاقة اختيار العملتين للتحويل
              CustomCard(
                color: theme.colorScheme.surfaceContainerHighest.withValues(alpha: 0.4),
                child: Column(
                  children: [
                    // التحويل من
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(color: Colors.red.withValues(alpha: 0.12), shape: BoxShape.circle),
                          child: const Icon(Icons.arrow_upward_rounded, color: Colors.red, size: 20),
                        ),
                        const SizedBox(width: 12),
                        const Expanded(child: Text('التحويل من محفظة:', style: TextStyle(fontWeight: FontWeight.bold))),
                        DropdownButton<String>(
                          value: _fromCurrency,
                          underline: const SizedBox(),
                          borderRadius: BorderRadius.circular(12),
                          items: availableCurrencies.map((c) {
                            return DropdownMenuItem(
                              value: c,
                              child: Text(
                                c == 'YER' ? 'ريال يمني (YER)' : c == 'SAR' ? 'ريال سعودي (SAR)' : 'دولار أمريكي (USD)',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            );
                          }).toList(),
                          onChanged: (val) {
                            if (val != null) {
                              setState(() => _fromCurrency = val);
                              _calculateRate();
                            }
                          },
                        ),
                      ],
                    ),
                    Padding(
                      padding: const EdgeInsets.only(right: 42, top: 2),
                      child: Align(
                        alignment: Alignment.centerRight,
                        child: Text(
                          'الرصيد المتاح: ${fromWallet.balance.toStringAsFixed(2)} ${fromWallet.currencySymbol}',
                          style: TextStyle(fontSize: 12, color: theme.colorScheme.primary, fontWeight: FontWeight.bold),
                        ),
                      ),
                    ),

                    // زر التبديل السريع
                    Center(
                      child: IconButton(
                        onPressed: _swapCurrencies,
                        icon: Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(color: theme.primaryColor, shape: BoxShape.circle),
                          child: const Icon(Icons.swap_vert_rounded, color: Colors.white, size: 20),
                        ),
                      ),
                    ),

                    // التحويل إلى
                    Row(
                      children: [
                        Container(
                          padding: const EdgeInsets.all(8),
                          decoration: BoxDecoration(color: Colors.green.withValues(alpha: 0.12), shape: BoxShape.circle),
                          child: const Icon(Icons.arrow_downward_rounded, color: Colors.green, size: 20),
                        ),
                        const SizedBox(width: 12),
                        const Expanded(child: Text('التحويل إلى محفظة:', style: TextStyle(fontWeight: FontWeight.bold))),
                        DropdownButton<String>(
                          value: _toCurrency,
                          underline: const SizedBox(),
                          borderRadius: BorderRadius.circular(12),
                          items: availableCurrencies.map((c) {
                            return DropdownMenuItem(
                              value: c,
                              child: Text(
                                c == 'YER' ? 'ريال يمني (YER)' : c == 'SAR' ? 'ريال سعودي (SAR)' : 'دولار أمريكي (USD)',
                                style: const TextStyle(fontWeight: FontWeight.bold),
                              ),
                            );
                          }).toList(),
                          onChanged: (val) {
                            if (val != null) {
                              setState(() => _toCurrency = val);
                              _calculateRate();
                            }
                          },
                        ),
                      ],
                    ),
                  ],
                ),
              ),

              const SizedBox(height: 20),

              // حقل إدخال المبلغ
              AmountInputField(
                controller: _amountController,
                currencyCode: _fromCurrency,
                onChanged: (_) => _calculateRate(),
              ),

              const SizedBox(height: 16),

              // بطاقة ملخص الرسوم وسعر الصرف والمبلغ المستلم
              if (amount > 0) ...[
                FeeSummaryCard(
                  amount: amount,
                  fee: _calculatedFee,
                  currencySymbol: _fromCurrency,
                  feeDescription: _feeDescription,
                  targetAmount: _targetAmount,
                  targetCurrencySymbol: _toCurrency,
                  exchangeRate: _exchangeRate,
                  isLoading: _isCalculating,
                ),
                const SizedBox(height: 16),
              ],

              // ملاحظات
              TextFormField(
                controller: _noteController,
                decoration: const InputDecoration(
                  labelText: 'بيان / ملاحظات (اختياري)',
                  prefixIcon: Icon(Icons.notes_rounded),
                ),
              ),

              const SizedBox(height: 32),

              // زر التأكيد
              PrimaryButton(
                label: 'تأكيد التحويل والمصارفة',
                icon: Icons.swap_horiz_rounded,
                isLoading: _isLoading,
                onPressed: _submitExchange,
              ),
            ],
          ),
        ),
      ),
    );
  }
}
