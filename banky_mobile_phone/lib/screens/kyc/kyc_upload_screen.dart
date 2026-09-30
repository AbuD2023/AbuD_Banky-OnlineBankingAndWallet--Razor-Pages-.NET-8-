import 'dart:convert';
import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';
import 'package:provider/provider.dart';
import '../../core/auth_provider.dart';

/// شاشة رفع وتوثيق البطاقة الشخصية (KYC Upload Screen)
/// تتيح التقاط أو اختيار صورتي الوجه الأمامي والخلفي لبطاقة الهوية وإرسالها للمراجعة
class KycUploadScreen extends StatefulWidget {
  const KycUploadScreen({super.key});

  @override
  State<KycUploadScreen> createState() => _KycUploadScreenState();
}

class _KycUploadScreenState extends State<KycUploadScreen> {
  String? _frontBase64;
  String? _backBase64;
  bool _isFrontLoaded = false;
  bool _isBackLoaded = false;
  final ImagePicker _picker = ImagePicker();

  /// اختيار صورة الوجه الأمامي
  Future<void> _pickFrontImage(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(source: source, imageQuality: 70);
      if (picked != null) {
        final bytes = await picked.readAsBytes();
        setState(() {
          _frontBase64 = base64Encode(bytes);
          _isFrontLoaded = true;
        });
      }
    } catch (_) {
      // محاكاة صورة تجريبية في حال تشغيل بيئة لا تدعم الكاميرا
      setState(() {
        _frontBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRg==";
        _isFrontLoaded = true;
      });
    }
  }

  /// اختيار صورة الوجه الخلفي
  Future<void> _pickBackImage(ImageSource source) async {
    try {
      final picked = await _picker.pickImage(source: source, imageQuality: 70);
      if (picked != null) {
        final bytes = await picked.readAsBytes();
        setState(() {
          _backBase64 = base64Encode(bytes);
          _isBackLoaded = true;
        });
      }
    } catch (_) {
      setState(() {
        _backBase64 = "data:image/jpeg;base64,/9j/4AAQSkZJRg==";
        _isBackLoaded = true;
      });
    }
  }

  /// إرسال وثائق الهوية للإدارة
  Future<void> _handleSubmitKyc() async {
    if (_frontBase64 == null || _backBase64 == null) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('يرجى التقاط صورتي الوجه الأمامي والخلفي للهوية أولاً')),
      );
      return;
    }

    final auth = Provider.of<AuthProvider>(context, listen: false);
    final success = await auth.submitKyc(
      frontBase64: _frontBase64!,
      backBase64: _backBase64!,
    );

    if (!mounted) return;

    if (success) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('تم رفع صور الهوية بنجاح! طلبك قيد مراجعة الإدارة حالياً.'),
          backgroundColor: Colors.green,
        ),
      );
      Navigator.pop(context);
    } else {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text(auth.errorMessage ?? 'فشل رفع الوثائق'),
          backgroundColor: Colors.red,
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
        title: const Text('توثيق الهوية (KYC)'),
      ),
      body: SafeArea(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // حالة التوثيق الحالية
              _buildStatusCard(theme, user),
              const SizedBox(height: 24),

              Text(
                'رفع صور بطاقة الهوية الشخصية',
                style: TextStyle(
                  fontSize: 16,
                  fontWeight: FontWeight.bold,
                  color: theme.colorScheme.onSurface,
                ),
              ),
              const SizedBox(height: 4),
              Text(
                'تأكد من وضوح الصورة وتطابق الاسم مع بيانات حسابك ليتم قبولها من الإدارة سريعاً',
                style: TextStyle(
                  fontSize: 13,
                  color: theme.colorScheme.onSurface.withOpacity(0.6),
                ),
              ),
              const SizedBox(height: 20),

              // بطاقة الوجه الأمامي
              _buildImagePickerCard(
                theme: theme,
                title: 'صورة الوجه الأمامي للبطاقة',
                subtitle: 'يجب أن تظهر الصورة الشخصية والاسم الرباعي بوضوح',
                isLoaded: _isFrontLoaded,
                serverImagePath: user?.kycIdFront,
                onCamera: () => _pickFrontImage(ImageSource.camera),
                onGallery: () => _pickFrontImage(ImageSource.gallery),
              ),
              const SizedBox(height: 16),

              // بطاقة الوجه الخلفي
              _buildImagePickerCard(
                theme: theme,
                title: 'صورة الوجه الخلفي للبطاقة',
                subtitle: 'يجب أن يظهر الرقم الوطني وتاريخ الانتهاء بوضوح',
                isLoaded: _isBackLoaded,
                serverImagePath: user?.kycIdBack,
                onCamera: () => _pickBackImage(ImageSource.camera),
                onGallery: () => _pickBackImage(ImageSource.gallery),
              ),
              const SizedBox(height: 30),

              // زر الإرسال للمراجعة
              ElevatedButton.icon(
                onPressed: auth.isLoading ? null : _handleSubmitKyc,
                icon: const Icon(Icons.cloud_upload_rounded),
                label: auth.isLoading
                    ? const SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                      )
                    : Text(user?.isPendingApproval == true ? 'تحديث صور الهوية المرفوعة' : 'إرسال وثائق التوثيق للإدارة'),
                style: ElevatedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 16),
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// بطاقة حالة التوثيق الحالية
  Widget _buildStatusCard(ThemeData theme, dynamic user) {
    if (user?.isApproved == true) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.green.withOpacity(0.12),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: Colors.green),
        ),
        child: const Row(
          children: [
            Icon(Icons.verified_rounded, color: Colors.green, size: 32),
            SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('حسابك موثق ومفعل بالكامل', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.green)),
                  Text('يمكنك استخدام كافة خدمات التحويل والدفع ونقاط البيع بحرية.', style: TextStyle(fontSize: 12)),
                ],
              ),
            ),
          ],
        ),
      );
    }

    if (user?.isPendingApproval == true) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.amber.withOpacity(0.15),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: Colors.amber.shade700),
        ),
        child: Row(
          children: [
            Icon(Icons.hourglass_bottom_rounded, color: Colors.amber.shade900, size: 32),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('الطلب قيد المراجعة لدى الإدارة', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.amber.shade900)),
                  const Text('تم رفع صور البطاقة بنجاح، جاري فحصها والموافقة عليها من لوحة التحكم.', style: TextStyle(fontSize: 12)),
                ],
              ),
            ),
          ],
        ),
      );
    }

    if (user?.isRejected == true) {
      return Container(
        padding: const EdgeInsets.all(16),
        decoration: BoxDecoration(
          color: Colors.red.withOpacity(0.12),
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: Colors.red),
        ),
        child: Row(
          children: [
            const Icon(Icons.cancel_rounded, color: Colors.red, size: 32),
            const SizedBox(width: 12),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('تم رفض التوثيق السابق', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.red)),
                  Text('السبب: ${user?.kycRejectionReason ?? "الوثائق غير واضحة"}. يرجى التقاط صور جديدة.', style: const TextStyle(fontSize: 12)),
                ],
              ),
            ),
          ],
        ),
      );
    }

    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: Colors.blue.withOpacity(0.1),
        borderRadius: BorderRadius.circular(16),
        border: Border.all(color: Colors.blue.shade300),
      ),
      child: const Row(
        children: [
          Icon(Icons.info_outline_rounded, color: Colors.blue, size: 32),
          SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text('الحساب بانتظار التوثيق', style: TextStyle(fontWeight: FontWeight.bold, color: Colors.blue)),
                Text('قم برفع صورتي الهوية لتفعيل كافة الخدمات المصرفية.', style: TextStyle(fontSize: 12)),
              ],
            ),
          ),
        ],
      ),
    );
  }

  /// كرت اختيار صورة الوجه الأمامي أو الخلفي
  Widget _buildImagePickerCard({
    required ThemeData theme,
    required String title,
    required String subtitle,
    required bool isLoaded,
    String? serverImagePath,
    required VoidCallback onCamera,
    required VoidCallback onGallery,
  }) {
    return Container(
      padding: const EdgeInsets.all(16),
      decoration: BoxDecoration(
        color: theme.cardTheme.color,
        borderRadius: BorderRadius.circular(16),
        border: Border.all(
          color: isLoaded ? Colors.green : const Color(0xFFE2E8F0),
          width: isLoaded ? 1.5 : 1,
        ),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(title, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
              if (isLoaded)
                const Row(
                  children: [
                    Icon(Icons.check_circle, color: Colors.green, size: 16),
                    SizedBox(width: 4),
                    Text('تم اختيار الصورة', style: TextStyle(color: Colors.green, fontSize: 12, fontWeight: FontWeight.bold)),
                  ],
                )
              else if (serverImagePath != null)
                const Row(
                  children: [
                    Icon(Icons.cloud_done, color: Colors.blue, size: 16),
                    SizedBox(width: 4),
                    Text('صورة مرفوعة مسبقاً', style: TextStyle(color: Colors.blue, fontSize: 12)),
                  ],
                ),
            ],
          ),
          const SizedBox(height: 4),
          Text(subtitle, style: TextStyle(fontSize: 12, color: theme.colorScheme.onSurface.withOpacity(0.5))),
          const SizedBox(height: 12),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onCamera,
                  icon: const Icon(Icons.camera_alt_outlined, size: 18),
                  label: const Text('الكاميرا'),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                  ),
                ),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onGallery,
                  icon: const Icon(Icons.photo_library_outlined, size: 18),
                  label: const Text('المعرض'),
                  style: OutlinedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 10),
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                  ),
                ),
              ),
            ],
          ),
        ],
      ),
    );
  }
}
