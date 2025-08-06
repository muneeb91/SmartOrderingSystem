from flask import Flask, request, jsonify
from flask_cors import CORS
import logging
from textblob import TextBlob

app = Flask(__name__)
CORS(app)

# Configure logging
logging.basicConfig(level=logging.DEBUG, format='%(asctime)s - %(levelname)s - %(message)s')

@app.route('/analyze-feedback', methods=['POST'])
def analyze_feedback():
    try:
        data = request.get_json()
        comment = data.get('comment', '').strip()
        logging.debug(f"Received comment: '{comment}'")
        
        if not comment:
            logging.error("No comment provided")
            return jsonify({'error': 'Comment is required'}), 400
        
        # Split comment into sentences/clauses
        blob = TextBlob(comment)
        sentences = blob.sentences
        if not sentences:  # Fallback if sentence splitting fails
            sentences = [blob]
        
        sentiments = []
        for sentence in sentences:
            sentence_text = str(sentence).strip()
            polarity = sentence.sentiment.polarity
            sentiment = 'Positive' if polarity > 0 else 'Negative' if polarity < 0 else 'Neutral'
            # Extract nouns and adjectives as keywords
            keywords = [word.lower() for word, pos in sentence.tags if pos.startswith(('NN', 'JJ'))]
            sentiments.append({
                'sentence': sentence_text,
                'sentiment': sentiment,
                'keywords': keywords
            })
            logging.debug(f"Sentence: '{sentence_text}', Sentiment: {sentiment}, Keywords: {keywords}")
        
        # If only one sentence, return single sentiment for backward compatibility
        if len(sentences) == 1:
            response = {
                'sentiment': sentiments[0]['sentiment'],
                'keywords': sentiments[0]['keywords']
            }
        else:
            response = {'sentiments': sentiments}
        
        logging.debug(f"Response: {response}")
        return jsonify(response)
    
    except Exception as e:
        logging.error(f"Error analyzing feedback: {str(e)}")
        return jsonify({'error': f'Error analyzing feedback: {str(e)}'}), 500

if __name__ == '__main__':
    app.run(port=5001)