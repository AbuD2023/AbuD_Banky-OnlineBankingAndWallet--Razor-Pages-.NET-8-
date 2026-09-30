import 'package:flutter/material.dart';
import '../../models/pos_model.dart';
import '../../services/api_service.dart';

/// شاشة إدارة نقاط البيع الخاصة بالعميل (My POS Screen)
/// تتيح للعميل إنشاء نقطة بيع أو متجر، وتوليد كود الـ POS والـ QR لاستقبال المدفوعات من العملاء
class MyPosScreen extends StatefulWidget {
  const MyPosScreen({super.key});

  @override
  State<MyPosScreen> createState() => _MyPosScreenState();
}

class _MyPosScreenState extends State<MyPosScreen> {
  List<PosModel> _posList = [];
  bool _isLoading = false;

  @override
  void initState() {
    super.initState();
    _loadPosPoints();
  }

  Future<void> _loadPosPoints() async {
    setState(() => _isLoading = true);
    final list = await ApiService.getMyPosPoints();
    if (mounted) {
      setState(() {
        _posList = list;
        _isLoading = false;
      });
    }
  }

  /// فتح حوار إنشاء نقطة بيع جديدة
  void _openCreatePosDialog() {
    final nameController = TextEditingController();
    final addressController = TextEditingController();
    final categoryController = TextEditingController(text: 'متجر عام');

    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        title: const Row(
          children: [
            Icon(Icons.store_mall_directory_rounded, color: Colors.purple),
            SizedBox(width: 8),
            Text(
              'إنشاء نقطة بيع جديدة',
              style: TextStyle(fontWeight: FontWeight.bold, fontSize: 17),
            ),
          ],
        ),
        content: SingleChildScrollView(
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              TextField(
                controller: nameController,
                decoration: const InputDecoration(
                  labelText: 'اسم المتجر أو نقطة البيع',
                  hintText: 'مثال: سوبرماركت السعادة',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: categoryController,
                decoration: const InputDecoration(
                  labelText: 'نشاط أو تصنيف المتجر',
                  hintText: 'بقالة، مطعم، صيدلية، خدمات',
                ),
              ),
              const SizedBox(height: 12),
              TextField(
                controller: addressController,
                decoration: const InputDecoration(
                  labelText: 'العنوان أو الموقع (اختياري)',
                  hintText: 'الشارع، المدينة',
                ),
              ),
            ],
          ),
        ),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx),
            child: const Text('إلغاء'),
          ),
          ElevatedButton(
            onPressed: () async {
              final name = nameController.text.trim();
              if (name.isEmpty) return;

              Navigator.pop(ctx);
              final result = await ApiService.createPos(
                name: name,
                category: categoryController.text.trim(),
                address: addressController.text.trim().isNotEmpty
                    ? addressController.text.trim()
                    : null,
              );

              if (mounted) {
                if (result['success'] == true) {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(result['message'] ?? 'تم إنشاء نقطة البيع'),
                      backgroundColor: Colors.green,
                    ),
                  );
                  _loadPosPoints();
                } else {
                  ScaffoldMessenger.of(context).showSnackBar(
                    SnackBar(
                      content: Text(result['message'] ?? 'فشل الإنشاء'),
                      backgroundColor: Colors.red,
                    ),
                  );
                }
              }
            },
            child: const Text('إنشاء'),
          ),
        ],
      ),
    );
  }

  /// عرض كود نقطة البيع والـ QR لاستقبال الدفع
  void _showQrDialog(PosModel pos) {
    showDialog(
      context: context,
      builder: (ctx) => AlertDialog(
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(20)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              pos.name,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18),
            ),
            const SizedBox(height: 4),
            Text(
              pos.category ?? 'متجر',
              style: const TextStyle(color: Colors.grey, fontSize: 13),
            ),
            const SizedBox(height: 20),
            Container(
              padding: const EdgeInsets.all(20),
              decoration: BoxDecoration(
                color: Colors.grey.shade100,
                borderRadius: BorderRadius.circular(16),
                border: Border.all(color: Colors.grey.shade300),
              ),
              child: Column(
                children: [
                  const Icon(
                    Icons.qr_code_2_rounded,
                    size: 140,
                    color: Colors.purple,
                  ),
                  const SizedBox(height: 10),
                  Text(
                    pos.posCode,
                    style: const TextStyle(
                      fontFamily: 'monospace',
                      fontWeight: FontWeight.bold,
                      fontSize: 22,
                      letterSpacing: 2,
                      color: Colors.purple,
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),
            const Text(
              'اطلب من العميل إدخال هذا الكود أو مسح الـ QR في شاشة الدفع لتحويل المبلغ إليك مباشرة.',
              textAlign: TextAlign.center,
              style: TextStyle(fontSize: 12, color: Colors.grey),
            ),
            const SizedBox(height: 20),
            ElevatedButton(
              onPressed: () => Navigator.pop(ctx),
              child: const Text('إغلاق'),
            ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(
        title: const Text('نقاط البيع الخاصة بي (POS)'),
        actions: [
          IconButton(
            icon: const Icon(Icons.add_business_rounded),
            onPressed: _openCreatePosDialog,
            tooltip: 'إنشاء نقطة بيع جديدة',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadPosPoints,
        child: _isLoading
            ? const Center(child: CircularProgressIndicator())
            : _posList.isEmpty
            ? Center(
                child: Padding(
                  padding: const EdgeInsets.all(32),
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(
                        Icons.storefront_outlined,
                        size: 72,
                        color: Colors.grey.shade400,
                      ),
                      const SizedBox(height: 16),
                      const Text(
                        'ليس لديك أي نقطة بيع بعد',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'يمكنك إنشاء نقطة بيع لمتجرك أو نشاطك وتوليد كود خاص لاستقبال المدفوعات والتحويلات من العملاء مباشرة.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: Colors.grey, fontSize: 13),
                      ),
                      const SizedBox(height: 24),
                      ElevatedButton.icon(
                        onPressed: _openCreatePosDialog,
                        icon: const Icon(Icons.add_rounded),
                        label: const Text('إنشاء نقطة بيع الآن'),
                      ),
                    ],
                  ),
                ),
              )
            : ListView.separated(
                padding: const EdgeInsets.all(16),
                itemCount: _posList.length,
                separatorBuilder: (_, _) => const SizedBox(height: 12),
                itemBuilder: (ctx, index) {
                  final pos = _posList[index];
                  return Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: theme.cardTheme.color,
                      borderRadius: BorderRadius.circular(16),
                      boxShadow: [
                        BoxShadow(
                          color: Colors.black.withOpacity(0.04),
                          blurRadius: 8,
                          offset: const Offset(0, 2),
                        ),
                      ],
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          mainAxisAlignment: MainAxisAlignment.spaceBetween,
                          children: [
                            Row(
                              children: [
                                Container(
                                  padding: const EdgeInsets.all(10),
                                  decoration: BoxDecoration(
                                    color: Colors.purple.withOpacity(0.12),
                                    borderRadius: BorderRadius.circular(12),
                                  ),
                                  child: const Icon(
                                    Icons.store_rounded,
                                    color: Colors.purple,
                                    size: 24,
                                  ),
                                ),
                                const SizedBox(width: 12),
                                Column(
                                  crossAxisAlignment: CrossAxisAlignment.start,
                                  children: [
                                    Text(
                                      pos.name,
                                      style: const TextStyle(
                                        fontWeight: FontWeight.bold,
                                        fontSize: 15,
                                      ),
                                    ),
                                    Text(
                                      pos.category ?? 'متجر عام',
                                      style: TextStyle(
                                        color: theme.colorScheme.onSurface
                                            .withOpacity(0.5),
                                        fontSize: 12,
                                      ),
                                    ),
                                  ],
                                ),
                              ],
                            ),
                            IconButton(
                              icon: const Icon(
                                Icons.qr_code_2_rounded,
                                color: Colors.purple,
                                size: 28,
                              ),
                              onPressed: () => _showQrDialog(pos),
                              tooltip: 'عرض كود الدفع',
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
                                  'كود الـ POS للدفع',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: Colors.grey,
                                  ),
                                ),
                                Text(
                                  pos.posCode,
                                  style: const TextStyle(
                                    fontFamily: 'monospace',
                                    fontWeight: FontWeight.bold,
                                    fontSize: 14,
                                    color: Colors.purple,
                                  ),
                                ),
                              ],
                            ),
                            Column(
                              crossAxisAlignment: CrossAxisAlignment.end,
                              children: [
                                const Text(
                                  'إجمالي المبيعات المستلمة',
                                  style: TextStyle(
                                    fontSize: 11,
                                    color: Colors.grey,
                                  ),
                                ),
                                Text(
                                  '${pos.totalReceivedAmount.toStringAsFixed(2)} ر.ي',
                                  style: const TextStyle(
                                    fontWeight: FontWeight.bold,
                                    fontSize: 14,
                                    color: Colors.green,
                                  ),
                                ),
                              ],
                            ),
                          ],
                        ),
                      ],
                    ),
                  );
                },
              ),
      ),
    );
  }
}
