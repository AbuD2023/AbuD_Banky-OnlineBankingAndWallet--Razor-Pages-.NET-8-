import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';
import '../../models/wallet_model.dart';
import '../../models/transaction_model.dart';
import '../../services/api_service.dart';
import '../kyc/kyc_upload_screen.dart';
import '../transfer/transfer_by_phone_screen.dart';
import '../transfer/pay_pos_screen.dart';
import '../transfer/self_exchange_screen.dart';
import '../wallets/wallets_screen.dart';
import '../pos/my_pos_screen.dart';
import '../../widgets/quick_action_item.dart';

/// الشاشة الرئيسية للتطبيق (Home Dashboard)
/// تعرض بطاقات المحافظ بالعملات المختلفة، التحذير الدائم في حال عدم التوثيق، العمليات السريعة، وآخر الحركات
class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<TransactionModel> _recentTransactions = [];
  bool _isLoadingTrx = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  Future<void> _loadData() async {
    final auth = Provider.of<AuthProvider>(context, listen: false);
    await auth.refreshProfile();
    await auth.refreshWallets();

    setState(() => _isLoadingTrx = true);
    final trx = await ApiService.getTransactions();
    if (mounted) {
      setState(() {
        _recentTransactions = trx.take(10).toList();
        _isLoadingTrx = false;
      });
    }
  }

  /// التحقق من شرط توثيق الحساب قبل السماح بإجراء أي عملية
  void _executeWithKycCheck(VoidCallback onApproved) {
    final auth = Provider.of<AuthProvider>(context, listen: false);
    if (auth.isKycApproved) {
      onApproved();
    } else {
      showDialog(
        context: context,
        builder: (ctx) => AlertDialog(
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(20),
          ),
          title: const Row(
            children: [
              Icon(Icons.warning_amber_rounded, color: Colors.orange, size: 28),
              SizedBox(width: 8),
              Text(
                'الحساب غير موثق',
                style: TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
              ),
            ],
          ),
          content: Text(
            auth.user?.isPendingApproval == true
                ? 'وثائقك قيد المراجعة حالياً من قبل إدارة النظام. يرجى الانتظار لحين الموافقة لتتمكن من استخدام كافة الخدمات المالية.'
                : 'لا يمكن استخدام أي ميزة أو عملية مالية في التطبيق إلا بعد توثيق الحساب برفع صورة البطاقة الشخصية (الوجهين) والموافقة عليها من الإدارة.',
            style: const TextStyle(fontSize: 14),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('إغلاق'),
            ),
            if (auth.user?.isPendingApproval != true)
              ElevatedButton(
                onPressed: () {
                  Navigator.pop(ctx);
                  Navigator.push(
                    context,
                    MaterialPageRoute(builder: (_) => const KycUploadScreen()),
                  ).then((_) => _loadData());
                },
                child: const Text('توثيق الحساب الآن'),
              ),
          ],
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);
    final auth = Provider.of<AuthProvider>(context);
    final user = auth.user;

    return Scaffold(
      appBar: AppBar(
        title: Row(
          children: [
            CircleAvatar(
              backgroundColor: theme.primaryColor.withOpacity(0.15),
              child: Icon(Icons.person_rounded, color: theme.primaryColor),
            ),
            const SizedBox(width: 10),
            Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'أهلاً بك،',
                  style: TextStyle(
                    fontSize: 12,
                    color: theme.colorScheme.onSurface.withOpacity(0.6),
                  ),
                ),
                Text(
                  user?.fullName ?? 'عميل Banky',
                  style: const TextStyle(
                    fontSize: 15,
                    fontWeight: FontWeight.bold,
                  ),
                ),
              ],
            ),
          ],
        ),
        actions: [
          IconButton(
            icon: const Icon(Icons.refresh_rounded),
            onPressed: _loadData,
            tooltip: 'تحديث البيانات',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadData,
        child: SingleChildScrollView(
          physics: const AlwaysScrollableScrollPhysics(),
          padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // 1. تنبيه التوثيق الدائم (Persistent KYC Warning Banner)
              if (!auth.isKycApproved)
                Container(
                  margin: const EdgeInsets.only(bottom: 16),
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    color: user?.isPendingApproval == true
                        ? Colors.amber.withOpacity(0.15)
                        : Colors.red.withOpacity(0.12),
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(
                      color: user?.isPendingApproval == true
                          ? Colors.amber.shade600
                          : Colors.red.shade400,
                      width: 1.5,
                    ),
                  ),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: [
                          Icon(
                            user?.isPendingApproval == true
                                ? Icons.hourglass_top_rounded
                                : Icons.error_outline_rounded,
                            color: user?.isPendingApproval == true
                                ? Colors.amber.shade900
                                : Colors.red.shade700,
                            size: 24,
                          ),
                          const SizedBox(width: 8),
                          Text(
                            user?.isPendingApproval == true
                                ? 'طلب التوثيق قيد المراجعة الإدارية'
                                : 'تنبيه: الحساب غير موثق بالهوية',
                            style: TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 14,
                              color: user?.isPendingApproval == true
                                  ? Colors.amber.shade900
                                  : Colors.red.shade800,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 8),
                      Text(
                        user?.isPendingApproval == true
                            ? 'تم إرسال صور الهوية الخاصة بك وهي بانتظار اعتماد الإدارة من لوحة التحكم لتفعيل كافة العمليات.'
                            : user?.isRejected == true
                            ? 'تم رفض توثيق حسابك: ${user?.kycRejectionReason ?? "يرجى إعادة رفع صور الهوية بوضوح"}. لا يمكن استخدام ميزات التطبيق إلا بعد التوثيق.'
                            : 'لا يمكنك استخدام أي ميزة أو إجراء تحويلات أو مدفوعات في التطبيق إلا بعد توثيق حسابك برفع صورة البطاقة الشخصية للوجه الأمامي والخلفي واعتمادها من الإدارة.',
                        style: TextStyle(
                          fontSize: 13,
                          color: user?.isPendingApproval == true
                              ? Colors.amber.shade900
                              : Colors.red.shade900,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Align(
                        alignment: Alignment.centerLeft,
                        child: ElevatedButton.icon(
                          onPressed: () {
                            Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (_) => const KycUploadScreen(),
                              ),
                            ).then((_) => _loadData());
                          },
                          icon: const Icon(Icons.upload_file_rounded, size: 18),
                          label: Text(
                            user?.isPendingApproval == true
                                ? 'معاينة حالة التوثيق'
                                : 'توثيق الحساب الآن',
                            style: const TextStyle(fontSize: 13),
                          ),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: user?.isPendingApproval == true
                                ? Colors.amber.shade800
                                : Colors.red.shade700,
                            padding: const EdgeInsets.symmetric(
                              horizontal: 16,
                              vertical: 8,
                            ),
                          ),
                        ),
                      ),
                    ],
                  ),
                ),

              // 2. شريط بطاقات المحافظ بالعملات المختلفة (Wallets Carousel)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'محافظي المالية',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  TextButton(
                    onPressed: () {
                      _executeWithKycCheck(() {
                        Navigator.push(
                          context,
                          MaterialPageRoute(
                            builder: (_) => const WalletsScreen(),
                          ),
                        ).then((_) => _loadData());
                      });
                    },
                    child: const Text('إدارة المحافظ'),
                  ),
                ],
              ),
              const SizedBox(height: 8),

              if (auth.wallets.isEmpty)
                Container(
                  padding: const EdgeInsets.all(24),
                  decoration: BoxDecoration(
                    color: theme.cardTheme.color,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: const Center(
                    child: Text(
                      'جاري تحميل المحافظ...',
                      style: TextStyle(color: Colors.grey),
                    ),
                  ),
                )
              else
                SizedBox(
                  height: 160,
                  child: ListView.separated(
                    scrollDirection: Axis.horizontal,
                    itemCount: auth.wallets.length,
                    separatorBuilder: (_, _) => const SizedBox(width: 12),
                    itemBuilder: (ctx, index) {
                      final wallet = auth.wallets[index];
                      return _buildWalletCard(theme, wallet);
                    },
                  ),
                ),
              const SizedBox(height: 24),

              // 3. أزرار العمليات المصرفية السريعة (Quick Actions)
              const Text(
                'الخدمات المصرفية السريعة',
                style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 12),
              SingleChildScrollView(
                scrollDirection: Axis.horizontal,
                child: Row(
                  children: [
                    QuickActionItem(
                      icon: Icons.send_rounded,
                      label: 'تحويل لمشترك',
                      color: Colors.blue,
                      onTap: () {
                        _executeWithKycCheck(() {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const TransferByPhoneScreen(),
                            ),
                          ).then((_) => _loadData());
                        });
                      },
                    ),
                    const SizedBox(width: 16),
                    QuickActionItem(
                      icon: Icons.swap_horiz_rounded,
                      label: 'بين حساباتي',
                      color: Colors.deepOrange,
                      onTap: () {
                        _executeWithKycCheck(() {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const SelfExchangeScreen(),
                            ),
                          ).then((_) => _loadData());
                        });
                      },
                    ),
                    const SizedBox(width: 16),
                    QuickActionItem(
                      icon: Icons.qr_code_scanner_rounded,
                      label: 'دفع لنقطة بيع',
                      color: Colors.purple,
                      onTap: () {
                        _executeWithKycCheck(() {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const PayPosScreen(),
                            ),
                          ).then((_) => _loadData());
                        });
                      },
                    ),
                    const SizedBox(width: 16),
                    QuickActionItem(
                      icon: Icons.add_circle_outline_rounded,
                      label: 'إيداع وتغذية',
                      color: Colors.teal,
                      onTap: () {
                        _executeWithKycCheck(() {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const WalletsScreen(),
                            ),
                          ).then((_) => _loadData());
                        });
                      },
                    ),
                    const SizedBox(width: 16),
                    QuickActionItem(
                      icon: Icons.storefront_rounded,
                      label: 'نقاط البيع',
                      color: Colors.orange,
                      onTap: () {
                        _executeWithKycCheck(() {
                          Navigator.push(
                            context,
                            MaterialPageRoute(
                              builder: (_) => const MyPosScreen(),
                            ),
                          ).then((_) => _loadData());
                        });
                      },
                    ),
                  ],
                ),
              ),
              const SizedBox(height: 28),

              // 4. سجل آخر العمليات (Recent Transactions)
              Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  const Text(
                    'آخر العمليات',
                    style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
                  ),
                  if (_recentTransactions.isNotEmpty)
                    Text(
                      '${_recentTransactions.length} عمليات',
                      style: TextStyle(
                        fontSize: 12,
                        color: theme.colorScheme.onSurface.withOpacity(0.5),
                      ),
                    ),
                ],
              ),
              const SizedBox(height: 10),

              if (_isLoadingTrx)
                const Center(
                  child: Padding(
                    padding: EdgeInsets.all(24),
                    child: CircularProgressIndicator(),
                  ),
                )
              else if (_recentTransactions.isEmpty)
                Container(
                  padding: const EdgeInsets.all(32),
                  decoration: BoxDecoration(
                    color: theme.cardTheme.color,
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Column(
                    children: [
                      Icon(
                        Icons.receipt_long_outlined,
                        size: 48,
                        color: Colors.grey.shade400,
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'لا توجد أي حركات مالية بعد',
                        style: TextStyle(color: Colors.grey),
                      ),
                    ],
                  ),
                )
              else
                ListView.separated(
                  shrinkWrap: true,
                  physics: const NeverScrollableScrollPhysics(),
                  itemCount: _recentTransactions.length,
                  separatorBuilder: (_, _) => const SizedBox(height: 8),
                  itemBuilder: (ctx, index) {
                    final trx = _recentTransactions[index];
                    return _buildTransactionTile(theme, trx);
                  },
                ),
            ],
          ),
        ),
      ),
    );
  }

  /// بطاقة المحفظة المالية الواحدة
  Widget _buildWalletCard(ThemeData theme, WalletModel wallet) {
    Gradient gradient;
    if (wallet.currencyCode == 'YER') {
      gradient = const LinearGradient(
        colors: [Color(0xFF0D6EFD), Color(0xFF0B5ED7)],
      );
    } else if (wallet.currencyCode == 'SAR') {
      gradient = const LinearGradient(
        colors: [Color(0xFF198754), Color(0xFF157347)],
      );
    } else {
      gradient = const LinearGradient(
        colors: [Color(0xFF212529), Color(0xFF343A40)],
      );
    }

    return Container(
      width: 260,
      padding: const EdgeInsets.all(18),
      decoration: BoxDecoration(
        gradient: gradient,
        borderRadius: BorderRadius.circular(20),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.15),
            blurRadius: 10,
            offset: const Offset(0, 4),
          ),
        ],
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(
                wallet.currencyNameAr,
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 14,
                ),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                decoration: BoxDecoration(
                  color: Colors.white.withOpacity(0.2),
                  borderRadius: BorderRadius.circular(8),
                ),
                child: Text(
                  wallet.currencyCode,
                  style: const TextStyle(
                    color: Colors.white,
                    fontWeight: FontWeight.bold,
                    fontSize: 11,
                  ),
                ),
              ),
            ],
          ),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text(
                'الرصيد المتاح',
                style: TextStyle(color: Colors.white70, fontSize: 11),
              ),
              const SizedBox(height: 2),
              Text(
                '${wallet.balance.toStringAsFixed(2)} ${wallet.currencySymbol}',
                style: const TextStyle(
                  color: Colors.white,
                  fontWeight: FontWeight.bold,
                  fontSize: 20,
                  letterSpacing: 0.5,
                ),
              ),
            ],
          ),
          Text(
            'حساب: ${wallet.accountNumber}',
            style: const TextStyle(
              color: Colors.white60,
              fontSize: 11,
              fontFamily: 'monospace',
            ),
          ),
        ],
      ),
    );
  }
}

/// عنصر الحركة المالية في السجل
Widget _buildTransactionTile(ThemeData theme, TransactionModel trx) {
  IconData icon;
  Color iconColor;

  if (trx.isIncoming) {
    icon = Icons.arrow_downward_rounded;
    iconColor = Colors.green;
  } else {
    icon = Icons.arrow_upward_rounded;
    iconColor = Colors.red;
  }

  return Container(
    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
    decoration: BoxDecoration(
      color: theme.cardTheme.color,
      borderRadius: BorderRadius.circular(14),
    ),
    child: Row(
      children: [
        Container(
          width: 42,
          height: 42,
          decoration: BoxDecoration(
            color: iconColor.withOpacity(0.12),
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icon, color: iconColor, size: 22),
        ),
        const SizedBox(width: 12),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                trx.typeNameAr,
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 13,
                ),
              ),
              Text(
                trx.isIncoming
                    ? 'من: ${trx.senderDisplayName ?? "إيداع"}'
                    : 'إلى: ${trx.receiverDisplayName ?? trx.posName ?? "تحويل"}',
                style: TextStyle(
                  fontSize: 11,
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                ),
              ),
            ],
          ),
        ),
        Column(
          crossAxisAlignment: CrossAxisAlignment.end,
          children: [
            Text(
              '${trx.isIncoming ? "+" : "-"}${trx.amount.toStringAsFixed(2)} ${trx.currencySymbol}',
              style: TextStyle(
                fontWeight: FontWeight.bold,
                fontSize: 13,
                color: iconColor,
              ),
            ),
            if (trx.createdAt != null)
              Text(
                '${trx.createdAt!.month}/${trx.createdAt!.day} ${trx.createdAt!.hour}:${trx.createdAt!.minute.toString().padLeft(2, '0')}',
                style: TextStyle(
                  fontSize: 10,
                  color: theme.colorScheme.onSurface.withOpacity(0.4),
                ),
              ),
          ],
        ),
      ],
    ),
  );
}
