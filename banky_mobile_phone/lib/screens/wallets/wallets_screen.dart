import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/wallet_model.dart';
import '../../services/api_service.dart';

/// شاشة إدارة المحافظ المالية متعددة العملات (Wallets Screen)
/// تتيح للعميل استعراض محافظه، فتح محفظة جديدة بعملة معتمدة، وتغذية الرصيد
class WalletsScreen extends StatefulWidget {
  const WalletsScreen({super.key});

  @override
  State<WalletsScreen> createState() => _WalletsScreenState();
}

class _WalletsScreenState extends State<WalletsScreen> {
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _refresh();
  }

  Future<void> _refresh() async {
    setState(() => _isLoading = true);
    final auth = Provider.of<AuthProvider>(context, listen: false);
    await auth.refreshWallets();
    setState(() => _isLoading = false);
  }

  /// فتح حوار إنشاء محفظة بعملة جديدة
  Future<void> _openCreateWalletSheet() async {
    final auth = Provider.of<AuthProvider>(context, listen: false);
    if (!auth.isKycApproved) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('يجب توثيق الحساب أولاً لفتح محافظ إضافية'),
        ),
      );
      return;
    }

    final availableCurrencies = await ApiService.getAvailableCurrencies();

    if (!mounted) return;

    if (availableCurrencies.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('لديك بالفعل محافظ بجميع العملات المتاحة حالياً'),
        ),
      );

      return;
    }

    showModalBottomSheet(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
      ),
      builder: (ctx) => Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Text(
              'فتح محفظة جديدة',
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 6),
            const Text(
              'اختر العملة التي ترغب بإنشاء محفظة مالية لها:',
              style: TextStyle(color: Colors.grey, fontSize: 13),
            ),
            const SizedBox(height: 16),
            Expanded(
              child: ListView.separated(
                itemCount: availableCurrencies.length,
                separatorBuilder: (_, _) => const Divider(),
                itemBuilder: (context, index) {
                  final curr = availableCurrencies[index];
                  return ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: CircleAvatar(
                      backgroundColor: Colors.blue.withOpacity(0.12),
                      child: Text(
                        curr.symbol,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.blue,
                        ),
                      ),
                    ),
                    title: Text(
                      '${curr.nameAr} (${curr.code})',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    subtitle: Text('1 ${curr.code} = ${curr.exchangeRate} ر.ي'),
                    trailing: ElevatedButton(
                      onPressed: () async {
                        Navigator.pop(ctx);
                        final result = await ApiService.createWallet(curr.code);
                        if (mounted) {
                          if (result['success'] == true) {
                            ScaffoldMessenger.of(this.context).showSnackBar(
                              SnackBar(
                                content: Text(
                                  result['message'] ?? 'تم إنشاء المحفظة بنجاح',
                                ),
                                backgroundColor: Colors.green,
                              ),
                            );
                            _refresh();
                          } else {
                            ScaffoldMessenger.of(this.context).showSnackBar(
                              SnackBar(
                                content: Text(
                                  result['message'] ?? 'فشل الإنشاء',
                                ),
                                backgroundColor: Colors.red,
                              ),
                            );
                          }
                        }
                      },
                      child: const Text('إنشاء'),
                    ),
                  );
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  /// فتح حوار تغذية وإيداع رصيد تجريبي
  // void _openDepositDialog(WalletModel wallet) {
  //   final amountController = TextEditingController(text: '10000');
  //   final noteController = TextEditingController(text: 'تغذية رصيد تجريبية');
  //   showDialog(
  //     context: context,
  //     builder: (ctx) => AlertDialog(
  //       shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
  //       title: Row(
  //         children: [
  //           const Icon(Icons.add_circle_outline, color: Colors.teal),
  //           const SizedBox(width: 8),
  //           Text(
  //             'إيداع في محفظة ${wallet.currencyNameAr}',
  //             style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
  //           ),
  //         ],
  //       ),
  //       content: Column(
  //         mainAxisSize: MainAxisSize.min,
  //         children: [
  //           Text(
  //             'رقم الحساب: ${wallet.accountNumber}',
  //             style: const TextStyle(
  //               fontSize: 12,
  //               color: Colors.grey,
  //               fontFamily: 'monospace',
  //             ),
  //           ),
  //           const SizedBox(height: 16),
  //           TextField(
  //             controller: amountController,
  //             keyboardType: const TextInputType.numberWithOptions(
  //               decimal: true,
  //             ),
  //             decoration: InputDecoration(
  //               labelText: 'مبلغ الإيداع (${wallet.currencySymbol})',
  //             ),
  //           ),
  //           const SizedBox(height: 12),
  //           TextField(
  //             controller: noteController,
  //             decoration: const InputDecoration(
  //               labelText: 'البيان أو الملاحظة',
  //             ),
  //           ),
  //         ],
  //       ),
  //       actions: [
  //         TextButton(
  //           onPressed: () => Navigator.pop(ctx),
  //           child: const Text('إلغاء'),
  //         ),
  //         ElevatedButton(
  //           onPressed: () async {
  //             final amt = double.tryParse(amountController.text.trim());
  //             if (amt == null || amt <= 0) return;
  //             Navigator.pop(ctx);
  //             final result = await ApiService.deposit(
  //               accountNumber: wallet.accountNumber,
  //               amount: amt,
  //               note: noteController.text.trim(),
  //             );
  //             if (mounted) {
  //               if (result['success'] == true) {
  //                 ScaffoldMessenger.of(context).showSnackBar(
  //                   SnackBar(
  //                     content: Text(result['message'] ?? 'تم الإيداع بنجاح'),
  //                     backgroundColor: Colors.green,
  //                   ),
  //                 );
  //                 _refresh();
  //               } else {
  //                 ScaffoldMessenger.of(context).showSnackBar(
  //                   SnackBar(
  //                     content: Text(result['message'] ?? 'فشل الإيداع'),
  //                     backgroundColor: Colors.red,
  //                   ),
  //                 );
  //               }
  //             }
  //           },
  //           child: const Text('تأكيد الإيداع'),
  //         ),
  //       ],
  //     ),
  //   );
  // }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('المحافظ متعددة العملات'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_card_rounded),
            onPressed: _openCreateWalletSheet,
            tooltip: 'فتح محفظة بعملة جديدة',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _refresh,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : ListView(
                padding: const EdgeInsets.all(16),
                children: [
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: theme.primaryColor.withOpacity(0.08),
                      borderRadius: BorderRadius.circular(16),
                      border: Border.all(
                        color: theme.primaryColor.withOpacity(0.2),
                      ),
                    ),
                    child: Row(
                      children: [
                        Icon(
                          Icons.currency_exchange_rounded,
                          color: theme.primaryColor,
                          size: 30,
                        ),
                        const SizedBox(width: 12),
                        const Expanded(
                          child: Text(
                            'يمكنك فتح محافظ بعملات مختلفة (ريال يمني، ريال سعودي، دولار أمريكي، إلخ) وإدارتها في مكان واحد.',
                            style: TextStyle(fontSize: 12),
                          ),
                        ),
                      ],
                    ),
                  ),
                  const SizedBox(height: 16),

                  ...auth.wallets.map((wallet) {
                    return Container(
                      margin: const EdgeInsets.only(bottom: 12),
                      padding: const EdgeInsets.all(18),
                      decoration: BoxDecoration(
                        color: theme.cardTheme.color,
                        borderRadius: BorderRadius.circular(18),
                        boxShadow: [
                          BoxShadow(
                            color: Colors.black.withOpacity(0.04),
                            blurRadius: 8,
                            offset: const Offset(0, 2),
                          ),
                        ],
                      ),
                      child: Column(
                        children: [
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Row(
                                children: [
                                  CircleAvatar(
                                    backgroundColor: Colors.blue.withOpacity(
                                      0.15,
                                    ),
                                    child: Text(
                                      wallet.currencySymbol,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        color: Colors.blue,
                                      ),
                                    ),
                                  ),
                                  const SizedBox(width: 12),
                                  Column(
                                    crossAxisAlignment:
                                        CrossAxisAlignment.start,
                                    children: [
                                      Text(
                                        wallet.currencyNameAr,
                                        style: const TextStyle(
                                          fontWeight: FontWeight.bold,
                                          fontSize: 15,
                                        ),
                                      ),
                                      Text(
                                        'رقم الحساب: ${wallet.accountNumber}',
                                        style: const TextStyle(
                                          fontSize: 12,
                                          color: Colors.grey,
                                          fontFamily: 'monospace',
                                        ),
                                      ),
                                    ],
                                  ),
                                ],
                              ),
                              Container(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 8,
                                  vertical: 3,
                                ),
                                decoration: BoxDecoration(
                                  color: Colors.green.withOpacity(0.12),
                                  borderRadius: BorderRadius.circular(8),
                                ),
                                child: Text(
                                  wallet.currencyCode,
                                  style: const TextStyle(
                                    color: Colors.green,
                                    fontWeight: FontWeight.bold,
                                    fontSize: 12,
                                  ),
                                ),
                              ),
                            ],
                          ),
                          const Divider(height: 24),
                          Row(
                            mainAxisAlignment: MainAxisAlignment.spaceBetween,
                            children: [
                              Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  const Text(
                                    'الرصيد المتاح',
                                    style: TextStyle(
                                      fontSize: 11,
                                      color: Colors.grey,
                                    ),
                                  ),
                                  Text(
                                    '${wallet.balance.toStringAsFixed(2)} ${wallet.currencySymbol}',
                                    style: const TextStyle(
                                      fontWeight: FontWeight.bold,
                                      fontSize: 18,
                                      color: Colors.green,
                                    ),
                                  ),
                                ],
                              ),
                              // ElevatedButton.icon(
                              //   onPressed: () => _openDepositDialog(wallet),
                              //   icon: const Icon(Icons.add, size: 16),
                              //   label: const Text('تغذية رصيد'),
                              //   style: ElevatedButton.styleFrom(
                              //     backgroundColor: Colors.teal,
                              //     padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 8),
                              //   ),
                              // ),
                            ],
                          ),
                        ],
                      ),
                    );
                  }),

                  const SizedBox(height: 12),
                  OutlinedButton.icon(
                    onPressed: _openCreateWalletSheet,
                    icon: const Icon(Icons.add_rounded),
                    label: const Text('فتح محفظة بعملة إضافية'),
                    style: OutlinedButton.styleFrom(
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(
                        borderRadius: BorderRadius.circular(14),
                      ),
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}
