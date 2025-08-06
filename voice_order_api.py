from flask import Flask, request, jsonify
from flask_cors import CORS
import difflib
import json
import re
import nltk
from nltk.tokenize import sent_tokenize
from nltk.corpus import stopwords
from nltk.sentiment.vader import SentimentIntensityAnalyzer

nltk.download('vader_lexicon')
nltk.download('punkt')
nltk.download('stopwords')

app = Flask(__name__)
CORS(app, resources={r"/*": {"origins": "http://localhost:8000"}})

@app.route('/parse_order', methods=['POST'])
def process_order():
    print("DEBUG: Received request to /parse_order")
    try:
        content_type = request.headers.get('Content-Type', '')
        if 'application/json' not in content_type.lower():
            print("DEBUG: Invalid Content-Type:", content_type)
            return jsonify({'error': 'Content-Type must be application/json'}), 400

        data = request.get_json(force=True)
        transcript = data.get('transcript', '').lower().strip()
        menu_items = data.get('menuItems', [])
        print(f"DEBUG: Received transcript: '{transcript}'")
        print(f"DEBUG: Received menuItems: {json.dumps(menu_items, indent=2)}")
        if not transcript:
            print("DEBUG: Empty transcript received")
            return jsonify({'error': 'Empty transcript provided'}), 400

        if not menu_items:
            print("DEBUG: No menuItems provided in request")
            return jsonify({'error': 'No menu items provided'}), 400

        items = []
        transcript_words = transcript.split()
        for item in menu_items:
            item_name = (item.get('name') or item.get('Name', '')).lower()
            item_id = item.get('id') or item.get('Id')
            item_price = item.get('price') if item.get('price') is not None else item.get('Price')
            if not item_name or item_id is None or item_price is None:
                print(f"DEBUG: Skipping invalid menu item: {json.dumps(item, indent=2)}")
                continue
            similarity = difflib.SequenceMatcher(None, item_name, transcript).ratio()
            quantity_match = re.search(r'(\d+)\s*(?:x\s*)?' + re.escape(item_name), transcript, re.IGNORECASE)
            count = int(quantity_match.group(1)) if quantity_match else max(1, transcript.count(item_name) or sum(1 for word in transcript_words if word in item_name))
            if item_name in transcript or any(word in item_name for word in transcript_words) or similarity > 0.7:
                items.append({
                    'menuItemId': item_id,
                    'name': item.get('name') or item.get('Name'),
                    'quantity': count,
                    'price': item_price
                })
                print(f"DEBUG: Matched item: {item.get('name') or item.get('Name')} (ID: {item_id}, Similarity: {similarity:.2f}, Quantity: {count})")

        if not items:
            print(f"DEBUG: No items matched for transcript: '{transcript}'")
            return jsonify({
                'items': [],
                'message': f'No items matched in transcript. Available items: {", ".join([item.get("name") or item.get("Name", "Unknown") for item in menu_items])}'
            }), 200

        print(f"DEBUG: Parsed items: {json.dumps(items, indent=2)}")
        return jsonify({'items': items}), 200

    except json.decoder.JSONDecodeError as json_error:
        print(f"DEBUG: JSON decode error: {str(json_error)}")
        return jsonify({'error': f'Failed to decode JSON: {str(json_error)}'}), 400
    except Exception as e:
        print(f"DEBUG: Unexpected error in /parse_order: {str(e)}")
        return jsonify({'error': f'Unexpected error: {str(e)}'}), 500

@app.route('/analyze_feedback', methods=['POST'])
def analyze_feedback():
    try:
        data = request.get_json()
        comment = data.get('comment', '')
        print(f"DEBUG: Received comment: '{comment}'")
        if not comment:
            return jsonify({'error': 'No comment provided'}), 400

        # Split comment into sentences/clauses
        sentences = sent_tokenize(comment)
        if not sentences:
            sentences = [comment]  # Fallback to whole comment if no sentences detected

        sid = SentimentIntensityAnalyzer()
        stop_words = set(stopwords.words('english'))
        sentiments = []

        for sentence in sentences:
            # Extract keywords (non-stopwords)
            words = nltk.word_tokenize(sentence.lower())
            keywords = [word for word in words if word.isalnum() and word not in stop_words]
            
            # Analyze sentiment
            scores = sid.polarity_scores(sentence)
            sentiment = 'positive' if scores['compound'] > 0.05 else 'negative' if scores['compound'] < -0.05 else 'neutral'
            
            sentiments.append({
                'sentence': sentence,
                'sentiment': sentiment,
                'keywords': keywords
            })

        print(f"DEBUG: Analyzed sentiments: {json.dumps(sentiments, indent=2)}")
        return jsonify({'sentiments': sentiments}), 200
    except Exception as e:
        print(f"DEBUG: Error in /analyze_feedback: {str(e)}")
        return jsonify({'error': f'Unexpected error: {str(e)}'}), 500

if __name__ == '__main__':
    app.run(host='0.0.0.0', port=5000, debug=True)