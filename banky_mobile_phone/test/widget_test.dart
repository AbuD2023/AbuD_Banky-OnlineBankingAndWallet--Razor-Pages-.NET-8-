import 'package:flutter_test/flutter_test.dart';
import 'package:banky_mobile_phone/main.dart';

void main() {
  testWidgets('App smoke test', (WidgetTester tester) async {
    await tester.pumpWidget(const BankyApp());
    expect(find.byType(BankyApp), findsOneWidget);
    await tester.pump(const Duration(seconds: 2));
    await tester.pump();
  });
}
